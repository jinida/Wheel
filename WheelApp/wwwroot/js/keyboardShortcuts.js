// Initialize wheelApp namespace
window.wheelApp = window.wheelApp || {};

wheelApp.initializeGridKeyListener = function (elementId, dotNetRef) {
    const element = document.getElementById(elementId);
    if (!element) {
        console.error(`Element with id '${elementId}' not found`);
        return;
    }

    // Store dotNetRef globally for document-level listener
    window._gridKeyboardDotNetRef = dotNetRef;

    // List of keys to capture
    const targetKeys = [
        'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight',  // Navigation
        'Digit1', 'Digit2', 'Digit3', 'Digit4', 'Digit5', 'Digit6', 'Digit7', 'Digit8', 'Digit9', 'Digit0',  // Class selection
        'Backquote',  // Clear label (`)
        'KeyQ', 'KeyW', 'KeyE', 'KeyR',  // Role selection (Q=Train, W=Validation, E=Test, R=Initialize)
        'KeyZ', 'KeyX', 'KeyC',  // Additional shortcuts (Z, X, C)
        'Space', 'Escape', 'KeyA'  // Multi-selection helpers
    ];

    // Function to check if shortcuts should be disabled
    function shouldDisableShortcuts() {
        // Check if a modal is open (by checking for common modal classes/elements)
        const modalOpen = document.querySelector('.modal-overlay, .modal-backdrop, .modal.show, .modal-open, .blazored-modal-container, .modal-wrapper, .modal-content') !== null;
        if (modalOpen) return true;

        const activeElement = document.activeElement;

        // Only disable if focus is on a text input field
        if (activeElement) {
            const tagName = activeElement.tagName.toLowerCase();

            // Disable shortcuts if focus is on clickable/interactive elements
            // This allows normal browser navigation (Enter on links, Space on buttons, etc.)
            if (tagName === 'a' || tagName === 'button' || tagName === 'select') {
                return true;
            }

            // Check if it's a text input (not buttons, checkbox, radio, etc.)
            if (tagName === 'input') {
                const inputType = activeElement.type ? activeElement.type.toLowerCase() : 'text';
                // Only disable for text-like inputs (add more types for better coverage)
                const textInputTypes = ['text', 'password', 'email', 'search', 'tel', 'url', 'number', 'date', 'time', 'datetime-local', 'month', 'week', 'color', 'file', 'range'];
                // If type is not specified or is a text-input type, disable shortcuts
                if (!inputType || textInputTypes.includes(inputType)) {
                    return true;
                }
            }

            // Disable for textarea and contentEditable
            if (tagName === 'textarea' || activeElement.isContentEditable) {
                return true;
            }
        }

        // Enable shortcuts everywhere else (divs, canvas, etc.)
        return false;
    }

    // Global keyboard listener
    const keydownHandler = async (e) => {
        // CRITICAL: Only process events if we're on the image grid container
        // This prevents blocking navigation on other pages
        const gridContainer = document.getElementById(elementId);
        if (!gridContainer) {
            // Grid container not found - we're on a different page, don't process anything
            return;
        }

        // Check if DotNetObjectReference is still valid
        if (!window._gridKeyboardDotNetRef) {
            // DotNetRef disposed, listener should be cleaned up
            return;
        }

        // Skip if shortcuts should be disabled
        if (shouldDisableShortcuts()) {
            return;
        }

        // Handle Ctrl+C and Ctrl+V separately for copy/paste
        if (e.ctrlKey && (e.code === 'KeyC' || e.code === 'KeyV')) {
            e.preventDefault();
            try {
                const action = e.code === 'KeyC' ? 'copy' : 'paste';
                await window._gridKeyboardDotNetRef.invokeMethodAsync('HandleGlobalCopyPaste', action);
            } catch (error) {
                // Silently ignore errors - component may be disposed
                return;
            }
            return;
        }

        // Check if this is a key we want to handle
        if (targetKeys.includes(e.code) || (e.code === 'KeyA' && e.ctrlKey)) {
            // Prevent default browser behavior for these keys
            e.preventDefault();

            try {
                await window._gridKeyboardDotNetRef.invokeMethodAsync('OnKeyPressedWithModifiers', {
                    code: e.code,
                    ctrl: e.ctrlKey,
                    shift: e.shiftKey,
                    alt: e.altKey
                });
            } catch (error) {
                // Silently ignore errors - component may be disposed
                return;
            }
        }
    };

    // Remove any existing global listener to avoid duplicates
    if (window._gridKeydownHandler) {
        document.removeEventListener('keydown', window._gridKeydownHandler);
    }

    // Store reference to handler for cleanup
    window._gridKeydownHandler = keydownHandler;

    // Add global listener to document
    document.addEventListener('keydown', keydownHandler);

    console.log(`Global keyboard listener initialized for '${elementId}'`);
};

// Cleanup function to remove global listeners
wheelApp.cleanupGridKeyListener = function() {
    if (window._gridKeydownHandler) {
        document.removeEventListener('keydown', window._gridKeydownHandler);
        window._gridKeydownHandler = null;
    }
    window._gridKeyboardDotNetRef = null;
};

wheelApp.scrollToElement = function (containerId, elementSelector) {
    const container = document.getElementById(containerId);
    if (!container) {
        console.error(`Container with id '${containerId}' not found`);
        return;
    }

    const element = container.querySelector(elementSelector);
    if (!element) {
        console.warn(`Element with selector '${elementSelector}' not found in container '${containerId}'`);
        return;
    }

    // Scroll the element into view with smooth behavior, aligning to nearest edge
    element.scrollIntoView({ behavior: 'auto', block: 'nearest' });
};

wheelApp.makeResizable = function (containerId) {
    const container = document.getElementById(containerId);
    if (!container) {
        console.error(`Container with id '${containerId}' not found`);
        return;
    }

    const splitter = container.querySelector('.splitter');
    if (!splitter) {
        console.error(`Splitter not found in container '${containerId}'`);
        return;
    }

    const leftPanel = container.querySelector('.left-panel');
    const rightPanel = container.querySelector('.right-panel');

    let isDragging = false;
    let containerLeft = 0;
    let containerWidth = 0;
    let lastWidth = 0;
    let startWidth = 0;
    const minWidth = 300;
    const minRightWidth = 500;

    // Performance profiling
    let frameCount = 0;
    let totalTime = 0;
    let lastFrameTime = 0;
    let startTime = 0;

    // Pause all ResizeObservers during drag
    const pausedObservers = [];

    // Get initial width from CSS
    const computedStyle = window.getComputedStyle(leftPanel);
    let initialWidth = parseInt(computedStyle.width) || containerWidth * 0.4;

    splitter.addEventListener('mousedown', function (e) {
        e.preventDefault();
        isDragging = true;

        // Set global flag to prevent grid resize during drag
        window._splitterDragging = true;

        // Reset profiling
        frameCount = 0;
        totalTime = 0;
        startTime = performance.now();
        lastFrameTime = startTime;

        // Cache container dimensions once at drag start
        const rect = container.getBoundingClientRect();
        containerLeft = rect.left;
        containerWidth = container.offsetWidth;
        startWidth = leftPanel.getBoundingClientRect().width;

        // Clear any pending grid resize timeouts
        if (window._gridResizeTimeout) {
            clearTimeout(window._gridResizeTimeout);
            window._gridResizeTimeout = null;
        }

        // Add visual feedback
        document.body.style.cssText = 'cursor: col-resize; user-select: none;';
        splitter.classList.add('dragging');

        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);

        // Count grid rows for performance analysis
        const gridRows = document.querySelectorAll('.grid-row, .grid-cell');
        console.log('[SPLITTER] Drag started. Grid has', Math.floor(gridRows.length / 4), 'rows');
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

        // Direct grid template update - this triggers table resize immediately
        container.style.gridTemplateColumns = `${newLeftWidth}px 5px 1fr`;

        // Profiling
        const frameEnd = performance.now();
        const frameDuration = frameEnd - frameStart;
        const timeSinceLastFrame = frameStart - lastFrameTime;
        lastFrameTime = frameStart;

        frameCount++;
        totalTime += frameDuration;

        // Log slow frames
        if (frameDuration > 5) {
            console.warn(`[SPLITTER] Slow frame #${frameCount}: ${frameDuration.toFixed(2)}ms (gap: ${timeSinceLastFrame.toFixed(2)}ms)`);
        }
    }

    function onMouseUp() {
        if (!isDragging) return;
        isDragging = false;

        // Clear global flag
        window._splitterDragging = false;

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

    console.log(`Resizable splitter initialized for '${containerId}'`);
};

wheelApp.initializeColumnResizing = function (gridId, autoFitOnLoad = true) {
    const grid = document.getElementById(gridId);
    if (!grid) {
        console.error('Grid not found:', gridId);
        return;
    }

    const loadingIndicator = document.getElementById('grid-loading-indicator');

    // Store resize observer for cleanup
    if (!window._gridResizeObservers) {
        window._gridResizeObservers = new Map();
    }

    // Auto-fit all columns on initial load
    if (autoFitOnLoad) {
        // Ensure loading indicator is visible and grid is hidden
        if (loadingIndicator) {
            loadingIndicator.classList.remove('hidden');
        }
        grid.classList.remove('ready');

        // Use setTimeout to ensure DOM is fully ready
        setTimeout(() => {
            const headers = grid.querySelectorAll('.grid-header');

            // Temporarily make grid visible to measure its actual width
            const originalDisplay = grid.style.display;
            grid.style.display = 'grid';
            grid.style.visibility = 'hidden'; // Keep it invisible but measurable

            const gridWidth = grid.clientWidth;

            // Restore original display state
            grid.style.display = originalDisplay;
            grid.style.visibility = '';

            console.log('[GRID INIT] Grid width:', gridWidth, 'Headers count:', headers.length);

            // Step 1: Auto-fit all columns to get ideal widths
            const idealWidths = [];
            headers.forEach((header, index) => {
                const width = autoFitColumnWidth(grid, index);
                idealWidths.push(width);
                console.log(`[GRID INIT] Column ${index} ideal width:`, width);
            });

            // Step 2: Calculate total ideal width
            let total = idealWidths.reduce((sum, w) => sum + w, 0);
            console.log('[GRID INIT] Total ideal width:', total);

            // Step 3: Scale all columns to exactly match gridWidth (whether scaling up or down)
            const scale = gridWidth / total;
            console.log('[GRID INIT] Scaling by:', scale);

            const finalWidths = idealWidths.map(w => Math.floor(w * scale));

            // Step 4: Add remaining pixels to ensure exact match
            // Distribute remainder across columns instead of just adding to last column
            const scaledTotal = finalWidths.reduce((sum, w) => sum + w, 0);
            let remainder = gridWidth - scaledTotal;

            if (remainder !== 0) {
                console.log('[GRID INIT] Distributing remainder:', remainder);
                // Add 1px to each column until remainder is used up
                let colIndex = 0;
                while (remainder > 0) {
                    finalWidths[colIndex]++;
                    remainder--;
                    colIndex = (colIndex + 1) % finalWidths.length;
                }
                while (remainder < 0) {
                    finalWidths[colIndex]--;
                    remainder++;
                    colIndex = (colIndex + 1) % finalWidths.length;
                }
            }

            // Step 5: Apply final widths
            finalWidths.forEach((width, index) => {
                grid.style.setProperty(`--col-${index}-width`, `${width}px`);
                console.log(`[GRID INIT] Set column ${index} width to:`, width + 'px');
            });

            // Wait for next frame to ensure all measurements are done
            requestAnimationFrame(() => {
                // Wait one more frame to ensure CSS has updated
                requestAnimationFrame(() => {
                    // Hide loading indicator and show grid
                    if (loadingIndicator) {
                        loadingIndicator.classList.add('hidden');
                    }
                    grid.classList.add('ready');
                });
            });
        }, 0);
    } else {
        // If not auto-fitting, hide loading and show the grid immediately
        if (loadingIndicator) {
            loadingIndicator.classList.add('hidden');
        }
        grid.classList.add('ready');
    }

    const resizers = grid.querySelectorAll('.column-resizer');
    let currentResizer = null;
    let currentColumn = null;
    let startX = 0;
    let startLeftWidth = 0;
    let startRightWidth = 0;
    let lastClickTime = 0;

    resizers.forEach((resizer) => {
        resizer.addEventListener('mousedown', function (e) {
            e.stopPropagation(); // Prevent sorting when clicking on resizer
            e.preventDefault(); // Prevent text selection

            // Detect double-click
            const currentTime = new Date().getTime();
            const timeDiff = currentTime - lastClickTime;
            lastClickTime = currentTime;

            if (timeDiff < 300) {
                // Double-click detected - auto-fit column width
                const columnIndex = parseInt(resizer.dataset.column);
                autoFitColumn(grid, columnIndex);
                return;
            }

            currentResizer = resizer;
            const columnIndex = parseInt(resizer.dataset.column);
            currentColumn = columnIndex;

            // Get the current widths of both adjacent columns
            const leftWidthStr = grid.style.getPropertyValue(`--col-${columnIndex}-width`);
            const rightWidthStr = grid.style.getPropertyValue(`--col-${columnIndex + 1}-width`);

            startLeftWidth = leftWidthStr ? parseInt(leftWidthStr) : 150;
            startRightWidth = rightWidthStr ? parseInt(rightWidthStr) : 120;
            startX = e.pageX;

            // Add resizing class
            currentResizer.classList.add('resizing');
            document.body.style.cursor = 'col-resize';
            document.body.style.userSelect = 'none';

            // Attach move and up listeners
            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
        });
    });

    function onMouseMove(e) {
        if (!currentResizer) return;

        const diff = e.pageX - startX;
        const rightColumn = currentColumn + 1;

        // Total width of the two adjacent columns must remain constant
        const totalWidth = startLeftWidth + startRightWidth;

        // Calculate new widths - sum must equal totalWidth
        let newLeftWidth = startLeftWidth + diff;
        let newRightWidth = totalWidth - newLeftWidth;

        // Apply minimum constraints (50px each)
        if (newLeftWidth < 50) {
            newLeftWidth = 50;
            newRightWidth = totalWidth - newLeftWidth;
        } else if (newRightWidth < 50) {
            newRightWidth = 50;
            newLeftWidth = totalWidth - newRightWidth;
        }

        // Update both columns - other columns are NOT touched
        grid.style.setProperty(`--col-${currentColumn}-width`, `${newLeftWidth}px`);
        grid.style.setProperty(`--col-${rightColumn}-width`, `${newRightWidth}px`);
    }

    function onMouseUp() {
        if (currentResizer) {
            currentResizer.classList.remove('resizing');
            currentResizer = null;
            currentColumn = null;
        }

        document.body.style.cursor = '';
        document.body.style.userSelect = '';

        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
    }

    function autoFitColumnWidth(grid, columnIndex) {
        const headers = grid.querySelectorAll('.grid-header');
        const cells = grid.querySelectorAll(`.grid-cell:nth-child(${columnIndex + 1})`);

        let maxWidth = 0;

        // Create a temporary measurement element
        const measureEl = document.createElement('span');
        measureEl.style.cssText = `
            position: absolute;
            visibility: hidden;
            white-space: nowrap;
            font-family: var(--12-regular-font-family);
            font-weight: var(--12-regular-font-weight);
            font-size: var(--12-regular-font-size);
        `;
        document.body.appendChild(measureEl);

        // Measure header content width
        const header = headers[columnIndex];
        const headerContent = header.querySelector('.header-content');
        if (headerContent) {
            measureEl.textContent = headerContent.textContent || '';
            maxWidth = Math.max(maxWidth, measureEl.offsetWidth);
        }

        // Measure all cell content widths in this column
        cells.forEach(cell => {
            const cellSpan = cell.querySelector('span');
            const text = cellSpan ? cellSpan.textContent : cell.textContent;
            if (text) {
                measureEl.textContent = text;
                maxWidth = Math.max(maxWidth, measureEl.offsetWidth);
            }
        });

        // Clean up
        document.body.removeChild(measureEl);

        // Add padding: 12px left + 12px right = 24px base padding
        // Plus 16px buffer for the resizer and some breathing room
        let finalWidth = Math.max(50, maxWidth + 40);

        // Apply reasonable max constraints (more balanced)
        const maxConstraints = { 0: 250, 1: 180, 2: 180, 3: 200 };
        if (maxConstraints[columnIndex]) {
            finalWidth = Math.min(finalWidth, maxConstraints[columnIndex]);
        }

        return finalWidth;
    }

    function autoFitColumn(grid, columnIndex) {
        const headers = grid.querySelectorAll('.grid-header');
        const gridWidth = grid.clientWidth;

        if (gridWidth === 0) return;

        // Step 1: Auto-fit the target column
        const newWidth = autoFitColumnWidth(grid, columnIndex);

        // Step 2: Get current widths of all columns
        const currentWidths = [];
        headers.forEach((header, index) => {
            if (index === columnIndex) {
                currentWidths.push(newWidth);
            } else {
                const widthStr = grid.style.getPropertyValue(`--col-${index}-width`);
                currentWidths.push(widthStr ? parseInt(widthStr) : 150);
            }
        });

        // Step 3: Calculate total and scale to fit grid width
        let total = currentWidths.reduce((sum, w) => sum + w, 0);

        if (total !== gridWidth) {
            const scale = gridWidth / total;
            const scaledWidths = currentWidths.map(w => Math.floor(w * scale));

            // Adjust last column to match exactly
            const scaledTotal = scaledWidths.reduce((sum, w) => sum + w, 0);
            if (scaledTotal !== gridWidth) {
                scaledWidths[scaledWidths.length - 1] += (gridWidth - scaledTotal);
            }

            // Apply scaled widths
            scaledWidths.forEach((width, index) => {
                grid.style.setProperty(`--col-${index}-width`, `${width}px`);
            });

            console.log('[COLUMN AUTO-FIT] Column', columnIndex, 'fitted. New widths:', scaledWidths);
        } else {
            // Total matches exactly, just apply the new width
            grid.style.setProperty(`--col-${columnIndex}-width`, `${newWidth}px`);
            console.log('[COLUMN AUTO-FIT] Column', columnIndex, 'fitted to', newWidth + 'px');
        }
    }

    // Function to recalculate all column widths based on current grid width
    function recalculateAllColumns() {
        const headers = grid.querySelectorAll('.grid-header');
        const gridWidth = grid.clientWidth;

        if (gridWidth === 0) {
            // Grid not visible yet, skip recalculation
            return;
        }

        console.log('[GRID RESIZE] Recalculating columns for new grid width:', gridWidth);

        // Step 1: Get current column widths (if set), otherwise use ideal widths
        const currentWidths = [];
        let hasCustomWidths = false;

        headers.forEach((header, index) => {
            const currentWidthStr = grid.style.getPropertyValue(`--col-${index}-width`);
            if (currentWidthStr) {
                currentWidths.push(parseInt(currentWidthStr));
                hasCustomWidths = true;
            } else {
                currentWidths.push(autoFitColumnWidth(grid, index));
            }
        });

        // Step 2: Calculate total and adjust proportionally
        let total = currentWidths.reduce((sum, w) => sum + w, 0);

        // Step 3: Scale to match new grid width
        const scale = gridWidth / total;
        const newWidths = currentWidths.map(w => Math.floor(w * scale));

        // Step 4: Adjust last column to match exactly
        const newTotal = newWidths.reduce((sum, w) => sum + w, 0);
        if (newTotal !== gridWidth) {
            newWidths[newWidths.length - 1] += (gridWidth - newTotal);
        }

        // Step 5: Apply new widths
        newWidths.forEach((width, index) => {
            grid.style.setProperty(`--col-${index}-width`, `${width}px`);
        });

        console.log('[GRID RESIZE] New column widths:', newWidths);
    }

    // Set up ResizeObserver to detect grid width changes (e.g., when panel splitter is moved)
    const resizeObserver = new ResizeObserver(entries => {
        for (const entry of entries) {
            // Recalculate immediately for real-time resize during splitter drag
            recalculateAllColumns();
        }
    });

    // Observe the grid element for size changes
    resizeObserver.observe(grid);

    // Store observer for cleanup
    window._gridResizeObservers.set(gridId, resizeObserver);

    console.log(`Column resizing initialized for '${gridId}'`);
};

// Cleanup function for column resizing
wheelApp.cleanupColumnResizing = function (gridId) {
    // Cleanup ResizeObserver
    if (window._gridResizeObservers && window._gridResizeObservers.has(gridId)) {
        const observer = window._gridResizeObservers.get(gridId);
        observer.disconnect();
        window._gridResizeObservers.delete(gridId);
        console.log(`Column resizing cleaned up for '${gridId}'`);
    }

    // Cleanup resize timeout
    if (window._gridResizeTimeout) {
        clearTimeout(window._gridResizeTimeout);
        window._gridResizeTimeout = null;
    }
};
