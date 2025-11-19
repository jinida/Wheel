// Create wheelApp namespace if it doesn't exist
window.wheelApp = window.wheelApp || {};

window.wheelApp.triggerFileClick = function(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.click();
    } else {
        console.error("Element with id '" + elementId + "' not found.");
    }
}

window.wheelApp.downloadFile = function(fileName, contentType, content) {
    const blob = new Blob([content], { type: contentType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
}