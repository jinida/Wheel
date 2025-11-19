// Create wheelApp namespace if it doesn't exist
window.wheelApp = window.wheelApp || {};

window.wheelApp.makeResizable = function (containerId) {
    const container = document.getElementById(containerId);
    if (!container) return;
    const splitter = container.querySelector('.splitter');
    if (!splitter) return;

    const leftPanel = container.querySelector('.left-panel');
    const rightPanel = container.querySelector('.right-panel');

    let isDragging = false;
    let containerLeft = 0;
    let containerWidth = 0;
    let lastWidth = 0;
    const minWidth = 300;
    const minRightWidth = 500;

    // Performance profiling
    let frameCount = 0;
    let totalTime = 0;
    let lastFrameTime = 0;
    let startTime = 0;

    // Pause all ResizeObservers during drag
    const pausedObservers = [];

    splitter.addEventListener('mousedown', function (e) {
        e.preventDefault();
        isDragging = true;

        // Reset profiling
        frameCount = 0;
        totalTime = 0;
        startTime = performance.now();
        lastFrameTime = startTime;

        // Cache container dimensions once at drag start
        const rect = container.getBoundingClientRect();
        containerLeft = rect.left;
        containerWidth = container.offsetWidth;

        // Pause all ResizeObservers to prevent expensive callbacks during drag
        resizeObservers.forEach((observer, element) => {
            observer.disconnect();
            pausedObservers.push({ observer, element });
        });

        // Disable transitions during drag for instant updates
        container.style.transition = 'none';
        if (leftPanel) leftPanel.style.transition = 'none';
        if (rightPanel) rightPanel.style.transition = 'none';

        // Add visual feedback
        document.body.style.cssText = 'cursor: col-resize; user-select: none; pointer-events: none;';
        container.style.pointerEvents = 'auto';
        splitter.style.pointerEvents = 'auto';
        splitter.classList.add('dragging');

        // Use non-passive for better control
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);

        console.log('[SPLITTER] Drag started');
    });

    function onMouseMove(e) {
        if (!isDragging) return;

        const frameStart = performance.now();

        // Calculate new width
        let newLeftWidth = e.clientX - containerLeft;

        // Clamp to bounds
        const maxWidth = containerWidth - minRightWidth - 5;
        if (newLeftWidth < minWidth) newLeftWidth = minWidth;
        else if (newLeftWidth > maxWidth) newLeftWidth = maxWidth;

        // Skip if width hasn't changed (reduce repaints)
        if (Math.abs(newLeftWidth - lastWidth) < 1) return;
        lastWidth = newLeftWidth;

        // Update grid template directly
        container.style.gridTemplateColumns = `${newLeftWidth}px 5px 1fr`;

        // Profiling
        const frameEnd = performance.now();
        const frameDuration = frameEnd - frameStart;
        const timeSinceLastFrame = frameStart - lastFrameTime;
        lastFrameTime = frameStart;

        frameCount++;
        totalTime += frameDuration;

        // Log slow frames
        if (frameDuration > 8) {
            console.warn(`[SPLITTER] Slow frame #${frameCount}: ${frameDuration.toFixed(2)}ms (gap: ${timeSinceLastFrame.toFixed(2)}ms)`);
        }
    }

    function onMouseUp() {
        if (!isDragging) return;
        isDragging = false;

        // Calculate performance stats
        const totalDuration = performance.now() - startTime;
        const avgFrameTime = frameCount > 0 ? (totalTime / frameCount) : 0;
        const fps = frameCount > 0 ? (frameCount / (totalDuration / 1000)) : 0;

        console.log('[SPLITTER] Drag completed:');
        console.log(`  Total frames: ${frameCount}`);
        console.log(`  Total duration: ${totalDuration.toFixed(2)}ms`);
        console.log(`  Average frame time: ${avgFrameTime.toFixed(2)}ms`);
        console.log(`  Average FPS: ${fps.toFixed(1)}`);
        console.log(`  Total update time: ${totalTime.toFixed(2)}ms`);

        // Re-enable transitions
        container.style.transition = '';
        if (leftPanel) leftPanel.style.transition = '';
        if (rightPanel) rightPanel.style.transition = '';

        // Resume all ResizeObservers
        pausedObservers.forEach(({ observer, element }) => {
            observer.observe(element);
        });
        pausedObservers.length = 0;

        // Remove visual feedback
        document.body.style.cssText = '';
        container.style.pointerEvents = '';
        splitter.style.pointerEvents = '';
        splitter.classList.remove('dragging');

        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);

        // Store final width in CSS variable for persistence
        const finalWidth = container.style.gridTemplateColumns.split(' ')[0];
        container.style.setProperty('--left-panel-width', finalWidth);

        // Trigger window resize event ONCE after drag completes
        window.dispatchEvent(new Event('resize'));
    }
}


const scrollbarTimers = {};

window.wheelApp.initializeCustomScrollbar = function (elementId) {
    const scrollableElement = document.getElementById(elementId);
    if (!scrollableElement) {
        return;
    }

    scrollableElement.addEventListener('scroll', function () {
        if (scrollbarTimers[elementId]) {
            clearTimeout(scrollbarTimers[elementId]);
        }

        scrollableElement.classList.add('scrolling');

        scrollbarTimers[elementId] = setTimeout(() => {
            scrollableElement.classList.remove('scrolling');
        }, 2000);
    });
}

window.wheelApp.initializeKeyListener = function (dotNetObjectReference) {
    document.addEventListener('keydown', (e) => {
        if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Digit1', 'Digit2', 'Backquote', 'KeyQ', 'KeyW'].includes(e.code)) {
            e.preventDefault();
            dotNetObjectReference.invokeMethodAsync('OnKeyPressed', e.code);
        }
    });
}

// Note: initializeGridKeyListener and cleanupGridKeyListener are defined in keyboardShortcuts.js
// to avoid duplication and ensure single source of truth for keyboard event handling

window.wheelApp.scrollToElement = function (containerId, elementSelector) {
    const container = document.getElementById(containerId);
    if (!container) return;

    const element = container.querySelector(elementSelector);
    if (element) {
        element.scrollIntoView({ behavior: 'auto', block: 'nearest' });
    }
}

window.getImageDimensions = function (imageElement) {
    return [imageElement.naturalWidth, imageElement.naturalHeight];
}

window.getElementDimensions = function (element) {
    if (element) {
        return [element.clientWidth, element.clientHeight];
    }
    return [0, 0];
}

// Store ResizeObserver instances
const resizeObservers = new Map();

window.observeElementResize = function (element, dotNetRef, methodName) {
    if (!element) {
        return;
    }

    // Cleanup existing observer if present
    if (resizeObservers.has(element)) {
        const existingObserver = resizeObservers.get(element);
        existingObserver.disconnect();
        resizeObservers.delete(element);
    }

    // Create new ResizeObserver
    const observer = new ResizeObserver((entries) => {
        for (const entry of entries) {
            const width = entry.contentRect.width;
            const height = entry.contentRect.height;

            // Invoke C# method with new dimensions
            dotNetRef.invokeMethodAsync(methodName, width, height)
                .catch(err => {
                    // Silently ignore - component may be disposed
                });
        }
    });

    observer.observe(element);
    resizeObservers.set(element, observer);
}

window.unobserveElementResize = function (element) {
    if (resizeObservers.has(element)) {
        const observer = resizeObservers.get(element);
        observer.disconnect();
        resizeObservers.delete(element);
    }
}

window.getElementBoundingRect = function (element) {
    const rect = element.getBoundingClientRect();
    return [rect.left, rect.top, rect.right, rect.bottom, rect.width, rect.height];
}

window.getImageCoordinates = function (imageElement, clientX, clientY, zoom, panX, panY) {
    // Get the actual displayed image bounds (after all transforms are applied)
    // getBoundingClientRect() already accounts for zoom and pan transforms
    const imageRect = imageElement.getBoundingClientRect();

    // Get image natural (original) dimensions
    const imgWidth = imageElement.naturalWidth;
    const imgHeight = imageElement.naturalHeight;

    // Mouse position relative to the displayed image's top-left corner
    const mouseX = clientX - imageRect.left;
    const mouseY = clientY - imageRect.top;

    // Calculate scale factor from displayed size to natural size
    // This accounts for object-fit: contain AND zoom/pan transforms
    const scaleX = imageRect.width / imgWidth;
    const scaleY = imageRect.height / imgHeight;

    // Convert from displayed pixel coordinates to natural image pixel coordinates
    const imageX = mouseX / scaleX;
    const imageY = mouseY / scaleY;

    // Clamp to image bounds and round
    const clampedX = Math.max(0, Math.min(Math.round(imageX), imgWidth - 1));
    const clampedY = Math.max(0, Math.min(Math.round(imageY), imgHeight - 1));

    return [clampedX, clampedY];
}

window.positionDropdown = function (dropdownId, targetButtonId) {
    const dropdown = document.getElementById(dropdownId);
    const button = document.getElementById(targetButtonId);

    if (!dropdown || !button) {
        console.error('Dropdown or button not found:', dropdownId, targetButtonId);
        return;
    }

    const buttonRect = button.getBoundingClientRect();

    // Position dropdown below and to the right of the button
    dropdown.style.top = (buttonRect.bottom + 4) + 'px';
    dropdown.style.left = (buttonRect.right - 140) + 'px'; // 140px is min-width of dropdown
}

window.setupDropdownOutsideClick = function (dropdownId, dotNetRef) {
    // Remove any existing listeners
    if (window.dropdownClickHandler) {
        document.removeEventListener('mousedown', window.dropdownClickHandler);
    }
    if (window.dropdownScrollHandler) {
        window.removeEventListener('scroll', window.dropdownScrollHandler, true);
    }

    const dropdown = document.getElementById(dropdownId);
    if (!dropdown) return;

    // Click/drag outside handler
    window.dropdownClickHandler = function (e) {
        // Check if click is outside the dropdown
        if (!dropdown.contains(e.target)) {
            dotNetRef.invokeMethodAsync('CloseDropdown');
            document.removeEventListener('mousedown', window.dropdownClickHandler);
            window.removeEventListener('scroll', window.dropdownScrollHandler, true);
            window.dropdownClickHandler = null;
            window.dropdownScrollHandler = null;
        }
    };

    // Scroll handler - close dropdown on any scroll
    window.dropdownScrollHandler = function (e) {
        // Close dropdown when scrolling
        dotNetRef.invokeMethodAsync('CloseDropdown');
        document.removeEventListener('mousedown', window.dropdownClickHandler);
        window.removeEventListener('scroll', window.dropdownScrollHandler, true);
        window.dropdownClickHandler = null;
        window.dropdownScrollHandler = null;
    };

    // Add listeners with a small delay to avoid immediate closure
    setTimeout(() => {
        document.addEventListener('mousedown', window.dropdownClickHandler);
        // Use capture phase (true) to catch scroll events on all elements
        window.addEventListener('scroll', window.dropdownScrollHandler, true);
    }, 100);
}

window.cleanupDropdownListener = function () {
    if (window.dropdownClickHandler) {
        document.removeEventListener('mousedown', window.dropdownClickHandler);
        window.dropdownClickHandler = null;
    }
    if (window.dropdownScrollHandler) {
        window.removeEventListener('scroll', window.dropdownScrollHandler, true);
        window.dropdownScrollHandler = null;
    }
}

// Global keyboard listener for canvas annotation shortcuts (z, x, c, r, Ctrl+C, Ctrl+V)
// Uses capture phase to ensure it works even when data-grid or other elements have focus
window.initializeGlobalCanvasShortcuts = function (dotNetRef) {
    // Remove existing listener if present
    if (window.globalCanvasShortcutHandler) {
        document.removeEventListener('keydown', window.globalCanvasShortcutHandler, true);
    }

    // Store the dotNetRef globally so we can verify it
    window.globalCanvasDotNetRef = dotNetRef;

    window.globalCanvasShortcutHandler = function (e) {
        // CRITICAL: Only process if ImageCanvas is present in DOM
        // This prevents blocking navigation on other pages
        const imageCanvas = document.querySelector('canvas[id^="canvas-"]');
        if (!imageCanvas) {
            // Not on Project page, don't process anything
            return;
        }

        // Check if DotNetObjectReference is still valid
        if (!window.globalCanvasDotNetRef) {
            // DotNetRef disposed, should be cleaned up
            return;
        }

        // Only handle if not inside an input/textarea/contenteditable
        const activeElement = document.activeElement;
        const isInputField = activeElement && (
            activeElement.tagName === 'INPUT' ||
            activeElement.tagName === 'TEXTAREA' ||
            activeElement.isContentEditable
        );

        // Don't interfere with input fields
        if (isInputField) return;

        const key = e.key.toLowerCase();

        try {
            // Handle Ctrl+C (copy) and Ctrl+V (paste)
            if (e.ctrlKey && (key === 'c' || key === 'v')) {
                e.preventDefault();
                const action = key === 'c' ? 'copy' : 'paste';
                dotNetRef.invokeMethodAsync('HandleGlobalCopyPaste', action)
                    .catch(err => {
                        // Silently ignore - component may be disposed
                    });
                return;
            }

            // Handle z, x, c, r keys (mode switching and import) - without Ctrl
            if (!e.ctrlKey && (key === 'z' || key === 'x' || key === 'c' || key === 'r')) {
                e.preventDefault();
                dotNetRef.invokeMethodAsync('HandleGlobalShortcut', key)
                    .catch(err => {
                        // Silently ignore - component may be disposed
                    });
            }
        } catch (error) {
            // Silently ignore errors
        }
    };

    // Use capture phase (true) to catch events before they reach child elements
    // This ensures the handler works even when data-grid or other elements have focus
    document.addEventListener('keydown', window.globalCanvasShortcutHandler, true);
}

// Cleanup function for global canvas shortcuts
// NOTE: Exposed both as window.cleanupGlobalCanvasShortcuts (for ImageCanvas.razor.cs)
// and as window.wheelApp.cleanupGlobalCanvasShortcuts (for KeyboardListenerHelper.cs)
window.cleanupGlobalCanvasShortcuts = function () {
    if (window.globalCanvasShortcutHandler) {
        document.removeEventListener('keydown', window.globalCanvasShortcutHandler, true);
        window.globalCanvasShortcutHandler = null;
        window.globalCanvasDotNetRef = null;
    }
}

// Alias for consistency with wheelApp namespace
window.wheelApp.cleanupGlobalCanvasShortcuts = window.cleanupGlobalCanvasShortcuts;


window.wheelApp.initializeColumnResizing = function (gridId, autoFitOnLoad = true) {
    console.log('[GRID RESIZE] Starting initialization for:', gridId);
    const grid = document.getElementById(gridId);
    if (!grid) {
        console.error('Grid not found:', gridId);
        return;
    }
    console.log('[GRID RESIZE] Grid width:', grid.clientWidth);

    const loadingIndicator = document.getElementById('grid-loading-indicator');

    // Define column constraints (min and max widths)
    const columnConstraints = {
        0: { min: 100, max: 300 },  // FileName
        1: { min: 80, max: 200 },   // Label/Class
        2: { min: 80, max: 150 },   // Role
        3: { min: 100, max: 180 }   // Upload Date
    };

    // Auto-fit column function
    function autoFitColumn(columnIndex) {
        const headers = grid.querySelectorAll('.grid-header');
        const allCells = Array.from(grid.querySelectorAll('.grid-cell'));
        const columnCells = allCells.filter((cell, index) => (index % 4) === columnIndex);

        let maxWidth = 0;
        const constraint = columnConstraints[columnIndex] || { min: 50, max: 500 };

        const measureEl = document.createElement('span');
        measureEl.style.cssText = `
            position: absolute;
            visibility: hidden;
            white-space: nowrap;
            font-family: var(--12-regular-font-family, "Segoe UI", Tahoma, Geneva, Verdana, sans-serif);
            font-weight: var(--12-regular-font-weight, 400);
            font-size: var(--12-regular-font-size, 12px);
            padding: 0;
        `;
        document.body.appendChild(measureEl);

        // Measure header
        const header = headers[columnIndex];
        const headerContent = header.querySelector('.header-content');
        if (headerContent) {
            measureEl.textContent = headerContent.textContent || '';
            let headerWidth = measureEl.offsetWidth;
            const sortIndicator = headerContent.querySelector('.sort-indicator');
            if (sortIndicator) {
                headerWidth += 18;
            }
            maxWidth = Math.max(maxWidth, headerWidth);
        }

        // Sample cells
        const sampleSize = Math.min(columnCells.length, 100);
        const step = columnCells.length > sampleSize ? Math.floor(columnCells.length / sampleSize) : 1;

        for (let i = 0; i < columnCells.length; i += step) {
            const cell = columnCells[i];
            const cellSpan = cell.querySelector('span');
            const text = cellSpan ? cellSpan.textContent : cell.textContent;
            if (text && text.trim()) {
                measureEl.textContent = text;
                maxWidth = Math.max(maxWidth, measureEl.offsetWidth);
            }
        }

        document.body.removeChild(measureEl);

        let finalWidth = maxWidth + 70; // padding + resizer + buffer
        finalWidth = Math.max(constraint.min, Math.min(finalWidth, constraint.max));

        grid.style.setProperty(`--col-${columnIndex}-width`, `${finalWidth}px`);
        return finalWidth;
    }

    // Auto-fit all columns on initial load
    if (autoFitOnLoad) {
        if (loadingIndicator) {
            loadingIndicator.classList.remove('hidden');
        }
        grid.classList.remove('ready');

        setTimeout(() => {
            const headers = grid.querySelectorAll('.grid-header');
            const gridWidth = grid.clientWidth;

            // Step 1: Auto-fit all 4 columns to get ideal widths
            const idealWidths = [];
            for (let i = 0; i < 4; i++) {
                idealWidths[i] = autoFitColumn(i);
            }

            // Step 2: Calculate total ideal width
            let totalIdeal = idealWidths.reduce((sum, w) => sum + w, 0);

            // Step 3: Adjust to exactly match gridWidth
            // Start with ideal widths
            let columnWidths = [...idealWidths];
            let total = columnWidths.reduce((sum, w) => sum + w, 0);

            // If total exceeds gridWidth, scale down proportionally
            if (total > gridWidth) {
                const scale = gridWidth / total;
                for (let i = 0; i < 4; i++) {
                    const constraint = columnConstraints[i] || { min: 50, max: 500 };
                    let scaled = Math.floor(columnWidths[i] * scale);
                    columnWidths[i] = Math.max(constraint.min, Math.min(scaled, constraint.max));
                }
                total = columnWidths.reduce((sum, w) => sum + w, 0);
            }

            // Ensure last column respects its max constraint (180px)
            const col3Constraint = columnConstraints[3] || { min: 100, max: 180 };
            if (columnWidths[3] > col3Constraint.max) {
                // Redistribute excess to columns 0, 1, 2
                const excess = columnWidths[3] - col3Constraint.max;
                columnWidths[3] = col3Constraint.max;

                const perColumn = Math.floor(excess / 3);
                for (let i = 0; i < 3; i++) {
                    columnWidths[i] += perColumn;
                }

                // Add remainder to first column
                const remainder = excess - (perColumn * 3);
                columnWidths[0] += remainder;

                total = columnWidths.reduce((sum, w) => sum + w, 0);
            }

            // Final adjustment: ensure exact total = gridWidth
            if (total !== gridWidth) {
                // Add difference to last column
                const diff = gridWidth - total;
                columnWidths[3] += diff;

                // But respect min constraint
                if (columnWidths[3] < col3Constraint.min) {
                    const shortage = col3Constraint.min - columnWidths[3];
                    columnWidths[3] = col3Constraint.min;

                    // Take shortage from columns 0, 1, 2
                    const perColumn = Math.ceil(shortage / 3);
                    for (let i = 0; i < 3; i++) {
                        const constraint = columnConstraints[i] || { min: 50, max: 500 };
                        columnWidths[i] = Math.max(constraint.min, columnWidths[i] - perColumn);
                    }
                }
            }

            // Apply final widths
            for (let i = 0; i < 4; i++) {
                grid.style.setProperty(`--col-${i}-width`, `${columnWidths[i]}px`);
            }

            requestAnimationFrame(() => {
                requestAnimationFrame(() => {
                    if (loadingIndicator) {
                        loadingIndicator.classList.add('hidden');
                    }
                    grid.classList.add('ready');
                });
            });
        }, 50);
    } else {
        if (loadingIndicator) {
            loadingIndicator.classList.add('hidden');
        }
        grid.classList.add('ready');
    }

    // Column resizing
    const resizers = grid.querySelectorAll('.column-resizer');
    let currentColumn = null;
    let startX = 0;
    let startLeftWidth = 0;
    let startRightWidth = 0;
    let lastClickTime = 0;

    resizers.forEach((resizer) => {
        resizer.addEventListener('mousedown', function (e) {
            e.stopPropagation();
            e.preventDefault();

            const currentTime = new Date().getTime();
            const timeDiff = currentTime - lastClickTime;
            lastClickTime = currentTime;

            if (timeDiff < 300) {
                // Double-click: auto-fit both adjacent columns
                const leftCol = parseInt(resizer.dataset.column);
                const rightCol = leftCol + 1;

                autoFitColumn(leftCol);
                autoFitColumn(rightCol);
                return;
            }

            currentColumn = parseInt(resizer.dataset.column);
            const rightColumn = currentColumn + 1;

            // Get current widths from CSS variables
            const leftWidthStr = grid.style.getPropertyValue(`--col-${currentColumn}-width`);
            const rightWidthStr = grid.style.getPropertyValue(`--col-${rightColumn}-width`);

            startLeftWidth = leftWidthStr ? parseInt(leftWidthStr) : 150;
            startRightWidth = rightWidthStr ? parseInt(rightWidthStr) : 120;
            startX = e.pageX;

            resizer.classList.add('resizing');
            document.body.style.cursor = 'col-resize';
            document.body.style.userSelect = 'none';

            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
        });
    });

    function onMouseMove(e) {
        if (currentColumn === null) return;

        const diff = e.pageX - startX;
        const rightColumn = currentColumn + 1;

        // Total width of the two adjacent columns must remain constant
        const totalWidth = startLeftWidth + startRightWidth;

        // Get constraints for both columns
        const leftConstraint = columnConstraints[currentColumn] || { min: 50, max: 500 };
        const rightConstraint = columnConstraints[rightColumn] || { min: 50, max: 500 };

        // Calculate new widths - sum must equal totalWidth
        let newLeftWidth = startLeftWidth + diff;
        let newRightWidth = totalWidth - newLeftWidth;

        console.log(`[GRID RESIZE] Moving splitter ${currentColumn}: left=${newLeftWidth}, right=${newRightWidth}, total=${newLeftWidth + newRightWidth}`);

        // Apply left column constraints
        if (newLeftWidth < leftConstraint.min) {
            newLeftWidth = leftConstraint.min;
            newRightWidth = totalWidth - newLeftWidth;
        } else if (newLeftWidth > leftConstraint.max) {
            newLeftWidth = leftConstraint.max;
            newRightWidth = totalWidth - newLeftWidth;
        }

        // Apply right column constraints
        if (newRightWidth < rightConstraint.min) {
            newRightWidth = rightConstraint.min;
            newLeftWidth = totalWidth - newRightWidth;
            // Re-check left constraint
            if (newLeftWidth < leftConstraint.min) {
                newLeftWidth = leftConstraint.min;
                newRightWidth = totalWidth - newLeftWidth;
            } else if (newLeftWidth > leftConstraint.max) {
                newLeftWidth = leftConstraint.max;
                newRightWidth = totalWidth - newLeftWidth;
            }
        } else if (newRightWidth > rightConstraint.max) {
            newRightWidth = rightConstraint.max;
            newLeftWidth = totalWidth - newRightWidth;
            // Re-check left constraint
            if (newLeftWidth < leftConstraint.min) {
                newLeftWidth = leftConstraint.min;
                newRightWidth = totalWidth - newLeftWidth;
            } else if (newLeftWidth > leftConstraint.max) {
                newLeftWidth = leftConstraint.max;
                newRightWidth = totalWidth - newLeftWidth;
            }
        }

        // Update both columns - other columns are NOT touched
        grid.style.setProperty(`--col-${currentColumn}-width`, `${newLeftWidth}px`);
        grid.style.setProperty(`--col-${rightColumn}-width`, `${newRightWidth}px`);
    }

    function onMouseUp() {
        if (currentColumn !== null) {
            const resizer = grid.querySelector(`.column-resizer[data-column="${currentColumn}"]`);
            if (resizer) {
                resizer.classList.remove('resizing');
            }
            currentColumn = null;
        }

        document.body.style.cursor = '';
        document.body.style.userSelect = '';

        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
    }
}