using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace WheelApp.Components
{
    /// <summary>
    /// EditClassModal component - for editing existing project classes
    /// </summary>
    public partial class EditClassModal : ComponentBase
    {
        [Parameter]
        public bool IsVisible { get; set; }

        [Parameter]
        public EventCallback<Model> OnSubmit { get; set; }

        [Parameter]
        public EventCallback OnCancel { get; set; }

        private Model _model = new();
        private string? _nameError = null;
        private string? _colorError = null;

        public void Initialize(int id, string name, string color)
        {
            _model = new Model
            {
                Id = id,
                ClassName = name,
                ClassColor = color
            };
            _nameError = null;
            _colorError = null;
            StateHasChanged();
        }

        private async Task HandleValidSubmit()
        {
            _nameError = null;
            _colorError = null;
            await OnSubmit.InvokeAsync(_model);
        }

        public void SetValidationError(string propertyName, string error)
        {
            if (propertyName == "Name") _nameError = error;
            else if (propertyName == "Color") _colorError = error;
            StateHasChanged();
        }

        public void ClearForm()
        {
            _model = new();
            _nameError = null;
            _colorError = null;
        }

        private void OnNameInput()
        {
            _nameError = null;
        }

        private void OnColorInput()
        {
            _colorError = null;
        }

        private string GetNameInputClass() => string.IsNullOrEmpty(_nameError) ? "form-control" : "form-control is-invalid";
        private string GetColorInputClass() => string.IsNullOrEmpty(_colorError) ? "form-control color-text-input" : "form-control color-text-input is-invalid";

        private async Task Cancel()
        {
            _model = new();
            _nameError = null;
            _colorError = null;
            await OnCancel.InvokeAsync();
        }

        public class Model
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "Class name is required.")]
            public string? ClassName { get; set; }

            [Required(ErrorMessage = "Color is required.")]
            [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color (e.g., #FF5733)")]
            public string ClassColor { get; set; } = "#808080"; // Default gray
        }
    }
}
