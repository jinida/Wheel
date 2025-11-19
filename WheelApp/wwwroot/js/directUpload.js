// Direct file upload bypassing SignalR
window.directFileUpload = {
    // Upload files from an InputFile element by ID
    uploadFromInputElement: async function (inputElementId, projectName, description, batchSize, dotNetRef) {
        const inputElement = document.getElementById(inputElementId);
        if (!inputElement || !inputElement.files || inputElement.files.length === 0) {
            throw new Error('No files found in input element');
        }

        const files = Array.from(inputElement.files);
        const totalFiles = files.length;
        const batches = [];

        // Split files into batches
        for (let i = 0; i < files.length; i += batchSize) {
            batches.push(files.slice(i, i + batchSize));
        }

        console.log(`JS: Uploading ${totalFiles} files in ${batches.length} batches`);

        const allUploadedFiles = [];

        for (let i = 0; i < batches.length; i++) {
            const batch = batches[i];
            const formData = new FormData();
            formData.append('datasetName', projectName);
            formData.append('description', description || '');
            formData.append('projectName', projectName);

            batch.forEach(file => {
                console.log(`JS: Adding file to batch: ${file.name} (${file.size} bytes)`);
                formData.append('files', file);
            });


            try {
                const response = await fetch('/api/upload', {
                    method: 'POST',
                    body: formData
                });

                console.log(`JS: Batch ${i + 1} response status: ${response.status}`);

                if (!response.ok) {
                    const errorText = await response.text();
                    console.error(`JS: Batch ${i + 1} failed:`, errorText);
                    throw new Error(`Batch ${i + 1} failed: ${response.status} - ${errorText}`);
                }

                const result = await response.json();
                console.log(`JS: Batch ${i + 1} result:`, result);

                allUploadedFiles.push(...(result.uploadedFileNames || []));

                // Report progress to .NET
                if (dotNetRef) {
                    await dotNetRef.invokeMethodAsync('UpdateProgress', i + 1, batches.length, allUploadedFiles.length);
                }
            } catch (error) {
                console.error(`JS: Error in batch ${i + 1}:`, error);
                throw error;
            }
        }

        console.log(`JS: All batches complete. Total uploaded: ${allUploadedFiles.length}`);

        return {
            fileCount: allUploadedFiles.length,
            uploadedFileNames: allUploadedFiles
        };
    }
};
