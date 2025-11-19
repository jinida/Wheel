using WheelApp.Application.DTOs;

namespace WheelApp.Components
{
    /// <summary>
    /// ImageCanvas - Clipboard and Public API
    /// Handles annotation copying, pasting, and importing from other images
    /// </summary>
    public partial class ImageCanvas
    {
        /// <summary>
        /// Gets a copy of current annotations for importing to another image.
        /// </summary>
        public List<AnnotationDto> GetCurrentAnnotations()
        {
            return _annotations.Select(a => new AnnotationDto
            {
                Id = a.Id,
                classDto = a.classDto,
                Information = a.Information.Select(p => new Point2f ((float)p.X, (float)p.Y)).ToList(),
                CreatedAt = a.CreatedAt
            }).ToList();
        }

        /// <summary>
        /// Imports annotations from another image (used for V key and refresh button).
        /// </summary>
        public async Task ImportPreviousImageLabels()
        {
            // This will be called by the parent component to import labels
            // For now, just clear clipboard and use whatever is in clipboard
            if (_clipboard != null && _clipboard.Count > 0)
            {
                _annotationService.SelectedAnnotationIds.Clear();

                foreach (var clipboardAnnotation in _clipboard)
                {
                    // Create a new annotation from clipboard
                    var newAnnotation = new AnnotationDto
                    {
                        Id = 0,  // Will be assigned by database
                        classDto = clipboardAnnotation.classDto,
                        Information = clipboardAnnotation.Information.Select(p => new Point2f(p.X, p.Y)).ToList(),
                        CreatedAt = DateTime.Now
                    };

                    _annotations.Add(newAnnotation);

                    // Save to database based on annotation type (determined by point count)
                    if (newAnnotation.Information.Count == 2)
                    {
                        await OnBboxAdded.InvokeAsync(newAnnotation);
                    }
                    else if (newAnnotation.Information.Count >= 3)
                    {
                        await OnSegmentationAdded.InvokeAsync(newAnnotation);
                    }

                    // After save, the annotation Id should be updated - then select it
                    if (newAnnotation.Id > 0)
                    {
                        _annotationService.SelectedAnnotationIds.Clear();
                        _annotationService.SelectedAnnotationIds.Add(newAnnotation.Id);
                        _annotationService.SelectMultiple(new HashSet<int>(_annotationService.SelectedAnnotationIds));
                        Logger.LogInformation("[ImageCanvas] Pasted annotation with ID {Id}, auto-selected", newAnnotation.Id);
                    }
                }

                StateHasChanged();
            }
        }

        /// <summary>
        /// Imports annotations from a list (used by parent component).
        /// </summary>
        public async Task ImportAnnotations(List<AnnotationDto> annotations)
        {
            if (annotations == null || annotations.Count == 0) return;

            _annotationService.SelectedAnnotationIds.Clear();

            foreach (var sourceAnnotation in annotations)
            {
                // Create a deep copy of the annotation
                var newAnnotation = new AnnotationDto
                {
                    Id = 0,  // Will be assigned by database when saved
                    classDto = sourceAnnotation.classDto,
                    Information = sourceAnnotation.Information.Select(p => new Point2f ((float)p.X, (float)p.Y)).ToList(),
                    CreatedAt = DateTime.Now
                };

                _annotations.Add(newAnnotation);

                if (newAnnotation.Information.Count == 2)
                {
                    await OnBboxAdded.InvokeAsync(newAnnotation);
                }
                else if (newAnnotation.Information.Count >= 3)
                {
                    await OnSegmentationAdded.InvokeAsync(newAnnotation);
                }

                if (newAnnotation.Id > 0)
                {
                    _annotationService.SelectedAnnotationIds.Clear();
                    _annotationService.SelectedAnnotationIds.Add(newAnnotation.Id);
                    _annotationService.SelectMultiple(new HashSet<int>(_annotationService.SelectedAnnotationIds));
                    Logger.LogInformation("[ImageCanvas] Imported annotation from previous image with ID {Id}, auto-selected", newAnnotation.Id);
                }
            }

            StateHasChanged();
        }

        /// <summary>
        /// Called from JavaScript for Ctrl+C and Ctrl+V from anywhere on the page
        /// </summary>
        public async Task HandleGlobalCopyPaste(string action)
        {            // Block copy/paste for Classification/AnomalyDetection
            if (AreAnnotationShortcutsDisabled())
            {
                return;
            }

            if (action == "copy" && _annotationService.SelectedAnnotationIds.Count > 0)
            {
                _clipboard = new List<AnnotationDto>();
                foreach (var annotationId in _annotationService.SelectedAnnotationIds)
                {
                    var annotation = _annotations.FirstOrDefault(a => a.Id == annotationId);
                    if (annotation != null)
                    {
                        // Create a deep copy
                        _clipboard.Add(new AnnotationDto
                        {
                            Id = annotation.Id,
                            classDto = annotation.classDto,
                            Information = annotation.Information.Select(p => new Point2f ((float)p.X, (float)p.Y)).ToList(),
                            CreatedAt = annotation.CreatedAt
                        });
                    }
                }
            }
            else if (action == "paste" && _clipboard != null && _clipboard.Count > 0)
            {
                _annotationService.SelectedAnnotationIds.Clear();

                foreach (var clipboardAnnotation in _clipboard)
                {
                    // Create a new annotation from clipboard (paste at exact same position)
                    var newAnnotation = new AnnotationDto
                    {
                        Id = 0,  // Will be assigned by database
                        classDto = clipboardAnnotation.classDto,
                        Information = clipboardAnnotation.Information.Select(p => new Point2f ((float)p.X, (float)p.Y)).ToList(),
                        CreatedAt = DateTime.Now
                    };

                    _annotations.Add(newAnnotation);

                    // Save to database based on annotation type (determined by point count)
                    if (newAnnotation.Information.Count == 2)
                    {
                        await OnBboxAdded.InvokeAsync(newAnnotation);
                    }
                    else if (newAnnotation.Information.Count >= 3)
                    {
                        await OnSegmentationAdded.InvokeAsync(newAnnotation);
                    }

                    // After save, the annotation Id should be updated - then select it
                    if (newAnnotation.Id > 0)
                    {
                        _annotationService.SelectedAnnotationIds.Clear();
                        _annotationService.SelectedAnnotationIds.Add(newAnnotation.Id);
                        _annotationService.SelectMultiple(new HashSet<int>(_annotationService.SelectedAnnotationIds));
                        Logger.LogInformation("[ImageCanvas] Pasted annotation (Ctrl+V) with ID {Id}, auto-selected", newAnnotation.Id);
                    }
                }

                StateHasChanged();
            }
            else
            {
            }
        }
    }
}
