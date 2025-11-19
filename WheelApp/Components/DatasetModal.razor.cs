using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using WheelApp.Application.Common.Options;

namespace WheelApp.Components
{
    /// <summary>
    /// DatasetModal component - for creating new datasets
    /// Uses Application layer FileUploadOptions for validation (single source of truth)
    /// </summary>
    public partial class DatasetModal : ComponentBase
    {
        [Parameter]
        public bool IsVisible { get; set; }

        [Parameter]
        public EventCallback<Model> OnSubmit { get; set; }

        [Parameter]
        public EventCallback OnCancel { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        private IOptions<FileUploadOptions> FileUploadOptions { get; set; } = default!;

        private Model _model = new();
        private List<IBrowserFile> _uploadedFiles = new();
        private string? _validationError = null;

        private async Task HandleValidSubmit()
        {
            // Pass model to parent component along with uploaded files
            // Parent will handle dataset creation and image upload via MediatR
            _model.UploadedFiles = _uploadedFiles.ToList();
            await OnSubmit.InvokeAsync(_model);

            // Don't clear files immediately - parent needs them for upload
            // They will be cleared when modal opens next time or on cancel
        }

        /// <summary>
        /// Clear files - called from parent after successful upload
        /// </summary>
        public void ClearFiles()
        {
            _model = new();
            _uploadedFiles.Clear();
            _validationError = null;
        }

        /// <summary>
        /// Set validation error - called from parent when duplicate name error occurs
        /// </summary>
        public void SetValidationError(string error)
        {
            _validationError = error;
            StateHasChanged();
        }

        /// <summary>
        /// Clear validation error when user types
        /// </summary>
        private void OnNameInput(ChangeEventArgs e)
        {
            _validationError = null;
        }

        /// <summary>
        /// Get inline style for input - add red border when validation error exists
        /// </summary>
        private string GetInputStyle()
        {
            return string.IsNullOrEmpty(_validationError) ? "" : "border-color: #dc3545 !important;";
        }

        private async Task Cancel()
        {
            _model = new();
            _uploadedFiles.Clear();
            _validationError = null;
            await OnCancel.InvokeAsync();
        }

        public class Model
        {
            [Required(ErrorMessage = "Dataset name is required.")]
            public string? DatasetName { get; set; }
            public string? Description { get; set; }
            public List<IBrowserFile> UploadedFiles { get; set; } = new();
        }

        private void LoadFiles(InputFileChangeEventArgs e)
        {
            var allowedExtensions = FileUploadOptions.Value.AllowedExtensions;
            var maxFileSize = FileUploadOptions.Value.MaxFileSize;
            var newFiles = e.GetMultipleFiles(maximumFileCount: 10000);

            foreach (var newFile in newFiles)
            {
                var extension = Path.GetExtension(newFile.Name).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Validate file size
                if (newFile.Size > maxFileSize)
                {
                    continue;
                }

                if (!_uploadedFiles.Any(existingFile => existingFile.Name == newFile.Name))
                {
                    _uploadedFiles.Add(newFile);
                }
            }
            StateHasChanged();
        }

        private void RemoveFile(IBrowserFile file)
        {
            _uploadedFiles.Remove(file);
            StateHasChanged();
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblSByte = bytes;
            while (dblSByte >= 1024 && i < suffixes.Length - 1)
            {
                dblSByte /= 1024;
                i++;
            }
            return $"{dblSByte:0.##} {suffixes[i]}";
        }

        private async Task TriggerFolderUpload()
        {
            await JSRuntime.InvokeVoidAsync("wheelApp.triggerFileClick", "dataset-folder-upload-input");
        }

        private async Task TriggerFileUpload()
        {
            await JSRuntime.InvokeVoidAsync("wheelApp.triggerFileClick", "dataset-file-upload-input");
        }
    }
}
