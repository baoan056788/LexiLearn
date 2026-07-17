document.addEventListener('DOMContentLoaded', () => {
    const reader = document.getElementById('lectureDocument');
    if (!reader) return;

    const toolbar = document.getElementById('annotationToolbar');
    const notePopup = document.getElementById('notePopup');
    let currentSelection = null;
    let currentRange = null;

    // Load existing annotations
    const rawAnnotations = document.getElementById('annotationsData');
    if (rawAnnotations) {
        try {
            const annotations = JSON.parse(rawAnnotations.textContent);
            // In a real app we'd restore highlights here using XPath
            // For MVP we just load them, complex DOM restoration requires rangy or similar
        } catch (e) {
            console.error('Failed to parse annotations', e);
        }
    }

    reader.addEventListener('mouseup', (e) => {
        const selection = window.getSelection();
        if (selection.toString().trim().length > 0) {
            currentSelection = selection;
            currentRange = selection.getRangeAt(0);
            
            const rect = currentRange.getBoundingClientRect();
            
            toolbar.style.display = 'flex';
            toolbar.style.top = `${rect.top - 45 + window.scrollY}px`;
            toolbar.style.left = `${rect.left + (rect.width / 2) - (toolbar.offsetWidth / 2)}px`;
        } else {
            toolbar.style.display = 'none';
        }
    });

    // Hide toolbar when clicking elsewhere
    document.addEventListener('mousedown', (e) => {
        if (!toolbar.contains(e.target) && !notePopup.contains(e.target) && e.target.id !== 'lectureDocument' && !reader.contains(e.target)) {
            toolbar.style.display = 'none';
            notePopup.style.display = 'none';
        }
    });

    // Handle highlight button
    document.querySelectorAll('.color-btn').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            const color = e.target.dataset.color;
            if (currentRange) {
                const span = document.createElement('span');
                span.className = 'highlighted-text';
                span.style.backgroundColor = color;
                span.dataset.color = color;
                
                try {
                    currentRange.surroundContents(span);
                    window.getSelection().removeAllRanges();
                    toolbar.style.display = 'none';

                    // Save to server
                    await saveAnnotation({
                        lectureId: parseInt(document.getElementById('lectureId').value),
                        startOffset: currentRange.startOffset,
                        endOffset: currentRange.endOffset,
                        selectedText: span.textContent,
                        type: 'Highlight',
                        color: color
                    });
                } catch (err) {
                    console.error("Cannot highlight across multiple elements", err);
                    alert("Khong the highlight qua nhieu the cung luc. Vui long chon tung doan text.");
                }
            }
        });
    });

    async function saveAnnotation(data) {
        try {
            const response = await fetch('/api/Annotation/Save', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: JSON.stringify(data)
            });
            return await response.json();
        } catch (err) {
            console.error('Save failed', err);
        }
    }
});
