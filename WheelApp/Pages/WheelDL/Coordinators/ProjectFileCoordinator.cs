using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using WheelApp.Application.Common.Models;
using WheelApp.Extensions;
using WheelApp.Application.DTOs;
using WheelApp.Application.UseCases.Annotations.Commands.ImportAnnotations;
using WheelApp.Application.UseCases.Annotations.Queries.ExportAnnotations;
using WheelApp.Application.UseCases.Images.Commands.DeleteImages;
using WheelApp.Application.UseCases.Images.Commands.UploadImages;
using WheelApp.Application.UseCases.Projects.Queries.GetProjectWorkspace;
using WheelApp.Services;

namespace WheelApp.Pages.WheelDL.Coordinators
{
    /// <summary>
    /// Coordinator for file upload/download operations (images, annotations)
    /// Phase 2 Refactoring - Extracted from Project.razor.cs
    /// Refactored to use JSRuntimeExtensions and BaseProjectCoordinator for code reuse
    /// </summary>
    public class ProjectFileCoordinator : BaseProjectCoordinator
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ProjectWorkspaceCoordinator _workspaceCoordinator;

        public ProjectFileCoordinator(
            IMediator mediator,
            IJSRuntime jsRuntime,
            ProjectWorkspaceService workspaceService,
            ProjectWorkspaceCoordinator workspaceCoordinator)
            : base(mediator, workspaceService)
        {
            _jsRuntime = jsRuntime;
            _workspaceCoordinator = workspaceCoordinator;
        }

        /// <summary>
        /// Triggers file selection dialog via JSInterop
        /// </summary>
        public async Task TriggerFileDialogAsync(string inputId)
        {
            // Use JSRuntimeExtensions to eliminate duplicate try-catch
            await _jsRuntime.TryInvokeVoidAsync("wheelApp.triggerFileClick", inputId);
        }

        /// <summary>
        /// Uploads multiple image files to the dataset
        /// </summary>
        public async Task<FileUploadResult> UploadImagesAsync(InputFileChangeEventArgs e, int projectId, int datasetId)
        {
            var result = new FileUploadResult();

            try
            {
                var files = e.GetMultipleFiles(maximumFileCount: 1000);
                if (!files.Any())
                {
                    result.Success = false;
                    result.Message = "No files selected";
                    return result;
                }

                // Create file upload info list
                var fileInfos = new List<FileUploadInfo>();

                foreach (var file in files)
                {
                    using var browserStream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
                    var memoryStream = new MemoryStream();
                    await browserStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    fileInfos.Add(new FileUploadInfo
                    {
                        FileName = file.Name,
                        Stream = memoryStream,
                        FileSize = file.Size
                    });
                }

                // Send upload command
                var command = new UploadImagesCommand
                {
                    DatasetId = datasetId,
                    ProjectId = projectId,
                    Files = fileInfos
                };

                var uploadResult = await _mediator.Send(command);

                // Dispose streams
                foreach (var fileInfo in fileInfos)
                {
                    await fileInfo.Stream.DisposeAsync();
                }

                if (uploadResult.IsSuccess && uploadResult.Value != null)
                {
                    // Reload workspace data
                    await _workspaceCoordinator.ReloadWorkspaceAsync();

                    result.Success = true;
                    result.AddedCount = uploadResult.Value.AddedCount;
                    result.SkippedCount = uploadResult.Value.SkippedCount;
                    result.SkippedNames = uploadResult.Value.SkippedNames;

                    if (uploadResult.Value.SkippedCount > 0)
                    {
                        // Use base class helper to format truncated list (eliminates duplication)
                        string duplicateMessage = uploadResult.Value.SkippedCount == 1
                            ? $"'{uploadResult.Value.SkippedNames[0]}' already exists in the dataset."
                            : $"{uploadResult.Value.SkippedCount} images already exist: {FormatTruncatedList(uploadResult.Value.SkippedNames)}";

                        result.Message = $"Added {uploadResult.Value.AddedCount} images. {duplicateMessage}";
                    }
                    else if (uploadResult.Value.AddedCount > 0)
                    {
                        result.Message = $"Successfully added {uploadResult.Value.AddedCount} images.";
                    }
                }
                else
                {
                    // Use base class helper for error fallback (eliminates duplication)
                    result.Success = false;
                    result.Message = GetErrorOrDefault(uploadResult.Error, "Failed to add images.");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error adding images: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Imports annotations from JSON file
        /// </summary>
        public async Task<ImportResult> ImportAnnotationsAsync(InputFileChangeEventArgs e, int projectId)
        {
            var result = new ImportResult();

            var file = e.File;
            if (file == null || !file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                result.Success = false;
                result.Message = "Please select a valid JSON file.";
                return result;
            }

            try
            {
                // Read the JSON file content
                using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB limit
                using var reader = new StreamReader(stream);
                var jsonContent = await reader.ReadToEndAsync();

                if (string.IsNullOrEmpty(jsonContent))
                {
                    result.Success = false;
                    result.Message = "The JSON file is empty.";
                    return result;
                }

                // Send import command using MediatR
                var command = new ImportAnnotationsCommand
                {
                    ProjectId = projectId,
                    JsonContent = jsonContent
                };

                var importResult = await _mediator.Send(command);

                if (importResult.IsSuccess && importResult.Value != null)
                {
                    // Reload workspace data to reflect imported annotations
                    await _workspaceCoordinator.ReloadWorkspaceAsync();

                    result.Success = true;
                    result.ImportedCount = importResult.Value.ImportedCount;
                    result.FailedCount = importResult.Value.FailedCount;
                    result.SkippedCount = importResult.Value.SkippedCount;
                    result.FailedItems = importResult.Value.FailedItems;

                    if (importResult.Value.FailedCount > 0 || importResult.Value.SkippedCount > 0)
                    {
                        // Use base class helper to format truncated list (eliminates duplication)
                        var failedList = FormatTruncatedList(importResult.Value.FailedItems);

                        result.Message = $"Imported {importResult.Value.ImportedCount} labels. Failed: {importResult.Value.FailedCount}, Skipped: {importResult.Value.SkippedCount}. {(importResult.Value.FailedItems.Any() ? $"Failed items: {failedList}" : "")}";
                    }
                    else
                    {
                        result.Message = $"Successfully imported {importResult.Value.ImportedCount} labels.";
                    }
                }
                else
                {
                    // Use base class helper for error fallback (eliminates duplication)
                    result.Success = false;
                    result.Message = GetErrorOrDefault(importResult.Error, "Failed to import labels.");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error importing labels: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Exports annotations to JSON file
        /// </summary>
        public async Task<ExportResult> ExportAnnotationsAsync(int projectId, string? projectName)
        {
            var result = new ExportResult();

            try
            {
                // Use the ExportAnnotationsQuery from Application layer
                var query = new ExportAnnotationsQuery
                {
                    ProjectId = projectId
                };

                var exportResult = await _mediator.Send(query);

                if (exportResult.IsSuccess && exportResult.Value != null)
                {
                    // Serialize the export data to JSON
                    var jsonContent = JsonSerializer.Serialize(new
                    {
                        header = new
                        {
                            version = exportResult.Value.Header.Version,
                            type = exportResult.Value.Header.Type,
                            creator = exportResult.Value.Header.Creator,
                            categories = exportResult.Value.Header.Categories,
                            description = exportResult.Value.Header.Description
                        },
                        annotations = exportResult.Value.Annotations.Select(a => new
                        {
                            filename = a.Filename,
                            label = a.Label,
                            role = a.Role
                        })
                    }, new JsonSerializerOptions { WriteIndented = true });

                    var fileName = $"{projectName ?? "project"}_{DateTime.Now:yyyyMMdd_HHmmss}_annotations.json";

                    await _jsRuntime.InvokeVoidAsync("wheelApp.downloadFile", fileName, "application/json", jsonContent);

                    result.Success = true;
                    result.AnnotationCount = exportResult.Value.Annotations.Count;
                    result.Message = $"Exported {exportResult.Value.Annotations.Count} annotations.";
                }
                else
                {
                    // Use base class helper for error fallback (eliminates duplication)
                    result.Success = false;
                    result.Message = GetErrorOrDefault(exportResult.Error, "Failed to export annotations.");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error exporting labels: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Deletes selected images
        /// </summary>
        public async Task<DeleteResult> DeleteImagesAsync(List<int> imageIds, int projectId)
        {
            var result = new DeleteResult();

            if (imageIds.Count == 0)
            {
                result.Success = false;
                result.Message = "Please select images to delete.";
                return result;
            }

            // Use base class validation to eliminate duplication
            var contextResult = GetValidatedWorkspaceContext();
            if (!contextResult.IsSuccess)
            {
                result.Success = false;
                result.Message = contextResult.Error!;
                return result;
            }

            var context = contextResult.Value!;

            try
            {
                var command = new DeleteImagesCommand
                {
                    ImageIds = imageIds
                };

                var deleteResult = await _mediator.Send(command);

                if (deleteResult.IsSuccess)
                {
                    // Update local workspace by removing deleted images
                    var deletedImageIdSet = new HashSet<int>(imageIds);
                    context.Workspace.Images.RemoveAll(img => deletedImageIdSet.Contains(img.Id));

                    result.Success = true;
                    result.DeletedCount = deleteResult.Value?.DeletedCount ?? imageIds.Count;
                    result.Message = $"Successfully deleted {result.DeletedCount} images.";
                }
                else
                {
                    // Use base class helper for error fallback (eliminates duplication)
                    result.Success = false;
                    result.Message = GetErrorOrDefault(deleteResult.Error, "Failed to delete images.");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error deleting images: {ex.Message}";
            }

            return result;
        }
    }

    #region Result DTOs

    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int AddedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> SkippedNames { get; set; } = new();
    }

    public class ImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ImportedCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> FailedItems { get; set; } = new();
    }

    public class ExportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int AnnotationCount { get; set; }
    }

    public class DeleteResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int DeletedCount { get; set; }
        public int FailedCount { get; set; }
    }

    #endregion
}
