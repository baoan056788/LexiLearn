// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener("DOMContentLoaded", function () {
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    setTimeout(function () {
        document.querySelectorAll('.alert-dismissible').forEach(function (alert) {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        });
    }, 5000);

    const dictForm = document.getElementById('dictForm');
    const dictOffcanvasEl = document.getElementById('dictOffcanvas');
    let bsOffcanvas = null;

    if (dictOffcanvasEl) {
        bsOffcanvas = new bootstrap.Offcanvas(dictOffcanvasEl, { backdrop: false, scroll: true });
    }

    if (dictForm) {
        dictForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            const word = document.getElementById('dictInput').value.trim();
            if (!word) return;

            hideDictionaryMessages();
            document.getElementById('dictLoading').classList.remove('d-none');
            document.getElementById('dictSubmitBtn').disabled = true;

            try {
                const response = await fetch(`/api/ai/dictionary?term=${encodeURIComponent(word)}`);
                const data = await response.json().catch(() => ({}));

                if (!response.ok) {
                    if (response.status === 401 || response.status === 403) {
                        throw new Error('Vui lòng đăng nhập để dùng tra từ AI.');
                    }

                    throw new Error(data.message || 'Gemini chưa trả được kết quả. Vui lòng thử lại.');
                }

                renderDictionaryResult(data, word);
                document.getElementById('dictResult').classList.remove('d-none');
            } catch (error) {
                document.getElementById('dictErrorText').textContent = error.message || 'Đã có lỗi xảy ra.';
                document.getElementById('dictError').classList.remove('d-none');
            } finally {
                document.getElementById('dictLoading').classList.add('d-none');
                document.getElementById('dictSubmitBtn').disabled = false;
            }
        });
    }

    const resizeHandle = document.querySelector('.resize-handle');
    let isResizing = false;
    let currentWidth = parseInt(localStorage.getItem('dictWidth')) || 500;

    if (resizeHandle && dictOffcanvasEl) {
        dictOffcanvasEl.style.width = currentWidth + 'px';

        resizeHandle.addEventListener('mousedown', function () {
            isResizing = true;
            resizeHandle.classList.add('active');
            document.body.style.cursor = 'ew-resize';
            dictOffcanvasEl.style.transition = 'none';
        });

        document.addEventListener('mousemove', function (e) {
            if (!isResizing) return;

            const newWidth = window.innerWidth - e.clientX;
            if (newWidth > 300 && newWidth < window.innerWidth * 0.8) {
                currentWidth = newWidth;
                dictOffcanvasEl.style.width = currentWidth + 'px';
                if (document.getElementById('pinned-dict-tab') && dictOffcanvasEl.classList.contains('show')) {
                    document.body.style.marginRight = currentWidth + 'px';
                }
            }
        });

        document.addEventListener('mouseup', function () {
            if (!isResizing) return;

            isResizing = false;
            resizeHandle.classList.remove('active');
            document.body.style.cursor = '';
            dictOffcanvasEl.style.transition = '';
            localStorage.setItem('dictWidth', currentWidth);
        });
    }

    document.querySelectorAll('.pinned-tab a[href="#"]').forEach(a => {
        a.addEventListener('click', function (e) {
            e.preventDefault();
            if (bsOffcanvas) {
                bsOffcanvas.show();
            }
        });
    });

    const pinnedDictTab = document.getElementById('pinned-dict-tab');
    const btnPinDict = document.getElementById('btnPinDict');

    if (pinnedDictTab && bsOffcanvas) {
        if (btnPinDict) {
            btnPinDict.classList.replace('btn-outline-primary', 'btn-primary');
        }

        dictOffcanvasEl.style.transition = 'none';
        document.body.style.transition = 'none';
        bsOffcanvas.show();

        setTimeout(() => {
            document.body.style.marginRight = currentWidth + 'px';
            setTimeout(() => {
                dictOffcanvasEl.style.transition = '';
                document.body.style.transition = 'margin-right 0.3s ease-in-out';
            }, 50);
        }, 50);
    }

    if (dictOffcanvasEl) {
        dictOffcanvasEl.addEventListener('hidden.bs.offcanvas', function () {
            document.body.style.marginRight = '0';
        });

        dictOffcanvasEl.addEventListener('shown.bs.offcanvas', function () {
            if (document.getElementById('pinned-dict-tab')) {
                document.body.style.marginRight = currentWidth + 'px';
            }
        });
    }

    const dictSaveSetSelect = document.getElementById('dictSaveSetSelect');
    const dictSaveWordBtn = document.getElementById('dictSaveWordBtn');
    const dictSaveMsg = document.getElementById('dictSaveMsg');

    if (dictSaveSetSelect) {
        fetch('/api/VocabularySet/MySets')
            .then(res => res.json())
            .then(data => {
                dictSaveSetSelect.innerHTML = '<option value="">Chọn bộ từ...</option>';
                data.forEach(set => {
                    const opt = document.createElement('option');
                    opt.value = set.setId;
                    opt.textContent = set.title;
                    dictSaveSetSelect.appendChild(opt);
                });
            })
            .catch(() => dictSaveSetSelect.innerHTML = '<option value="">Lỗi tải dữ liệu</option>');

        if (dictSaveWordBtn) {
            dictSaveWordBtn.addEventListener('click', async function () {
                const setId = dictSaveSetSelect.value;
                if (!setId) {
                    showSaveMessage('Vui lòng chọn một bộ từ!', 'text-danger');
                    return;
                }

                dictSaveMsg.classList.add('d-none');
                dictSaveWordBtn.disabled = true;

                const examplesEl = document.getElementById('dictMeaningsContainer').querySelector('.fst-italic');
                const payload = {
                    SetId: parseInt(setId),
                    Term: document.getElementById('dictWord').textContent,
                    Meaning: document.getElementById('dictMeaning').textContent,
                    Ipa: document.getElementById('dictIpa').textContent.replace(/\//g, ''),
                    Example: examplesEl ? examplesEl.textContent.replace(/"/g, '') : ''
                };

                try {
                    const res = await fetch('/api/VocabularyCard/CreateAjax', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(payload)
                    });

                    if (!res.ok) {
                        throw new Error('Lỗi lưu từ');
                    }

                    showSaveMessage('Đã lưu thành công!', 'text-success fw-bold', true);
                    setTimeout(() => dictSaveMsg.classList.add('d-none'), 3000);
                } catch {
                    showSaveMessage('Có lỗi xảy ra, vui lòng thử lại.', 'text-danger');
                } finally {
                    dictSaveWordBtn.disabled = false;
                }
            });
        }
    }

    function hideDictionaryMessages() {
        document.getElementById('dictResult').classList.add('d-none');
        document.getElementById('dictError').classList.add('d-none');
        document.getElementById('dictSaveMsg')?.classList.add('d-none');
    }

    function renderDictionaryResult(data, fallbackWord) {
        document.getElementById('dictWord').textContent = data.word || fallbackWord;
        document.getElementById('dictIpa').textContent = data.ipa || '';
        document.getElementById('dictMeaning').textContent = data.vietnameseMeaning || '';

        const partOfSpeech = data.partOfSpeech || 'AI dictionary';
        let meaningsHtml = `<div class="mb-4">
            <h5 class="fw-bold text-secondary text-capitalize border-bottom pb-2 mb-3">
                <i class="fas fa-wand-magic-sparkles me-2"></i>${escapeHtml(partOfSpeech)}
            </h5>`;

        (data.definitions || []).slice(0, 4).forEach((definition, idx) => {
            meaningsHtml += `<div class="mb-3">
                <div class="fw-semibold"><span class="text-primary me-2">${idx + 1}.</span>${escapeHtml(definition.definition || '')}</div>`;

            if (definition.example) {
                meaningsHtml += `<div class="text-muted fst-italic mt-1 ms-4 border-start border-2 border-primary ps-2">"${escapeHtml(definition.example)}"</div>`;
            }

            if (definition.exampleVi) {
                meaningsHtml += `<div class="small text-secondary mt-1 ms-4">${escapeHtml(definition.exampleVi)}</div>`;
            }

            meaningsHtml += '</div>';
        });

        if (data.synonyms && data.synonyms.length > 0) {
            meaningsHtml += '<div class="mt-2 ms-4 mb-2"><strong class="text-dark">Đồng nghĩa: </strong>';
            meaningsHtml += data.synonyms.slice(0, 8).map(s => `<span class="badge bg-light text-dark border me-1">${escapeHtml(s)}</span>`).join('');
            meaningsHtml += '</div>';
        }

        if (data.antonyms && data.antonyms.length > 0) {
            meaningsHtml += '<div class="mt-2 ms-4 mb-2"><strong class="text-dark">Trái nghĩa: </strong>';
            meaningsHtml += data.antonyms.slice(0, 8).map(s => `<span class="badge bg-light text-dark border me-1">${escapeHtml(s)}</span>`).join('');
            meaningsHtml += '</div>';
        }

        if (data.note) {
            meaningsHtml += `<div class="alert alert-info small mt-3 mb-0"><i class="fas fa-circle-info me-1"></i>${escapeHtml(data.note)}</div>`;
        }

        meaningsHtml += '</div>';
        document.getElementById('dictMeaningsContainer').innerHTML = meaningsHtml;
    }

    function showSaveMessage(message, className, success = false) {
        dictSaveMsg.className = `small mt-2 ${className}`;
        dictSaveMsg.innerHTML = success ? `<i class="fas fa-check-circle me-1"></i>${message}` : message;
        dictSaveMsg.classList.remove('d-none');
    }

    function escapeHtml(value) {
        return String(value ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }
});
