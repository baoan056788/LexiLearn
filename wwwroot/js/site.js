// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Initialize tooltips
document.addEventListener("DOMContentLoaded", function () {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    });

    // Auto-hide alerts after 5 seconds
    setTimeout(function () {
        var alerts = document.querySelectorAll('.alert-dismissible');
        alerts.forEach(function (alert) {
            var bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        });
    }, 5000);

    // Dictionary logic
    const dictForm = document.getElementById('dictForm');
    const dictOffcanvasEl = document.getElementById('dictOffcanvas');
    let bsOffcanvas = null;
    if (dictOffcanvasEl) {
        bsOffcanvas = new bootstrap.Offcanvas(dictOffcanvasEl, { backdrop: false, scroll: true });
    }

    if (dictForm) {
        dictForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            const word = document.getElementById('dictInput').value.trim();
            if (!word) return;

            document.getElementById('dictResult').classList.add('d-none');
            document.getElementById('dictError').classList.add('d-none');
            document.getElementById('dictLoading').classList.remove('d-none');
            document.getElementById('dictSubmitBtn').disabled = true;

            try {
                const isVietnamese = /[àáãạảăắằẳẵặâấầẩẫậèéẹẻẽêềếểễệđìíĩỉịòóõọỏôốồổỗộơớờởỡợùúũụủưứừửữựỳýỹỷỵ]/i.test(word);
                let searchWordEn = word;
                let translatedVi = '';

                let transData = null;
                if (isVietnamese) {
                    const transRes = await fetch(`https://api.mymemory.translated.net/get?q=${encodeURIComponent(word)}&langpair=vi|en`);
                    transData = await transRes.json();
                    searchWordEn = transData.responseData.translatedText;
                    translatedVi = word;
                } else {
                    const transRes = await fetch(`https://api.mymemory.translated.net/get?q=${encodeURIComponent(word)}&langpair=en|vi`);
                    transData = await transRes.json();
                    translatedVi = transData.responseData.translatedText;
                    searchWordEn = word;
                }

                // If translation returned identical word, maybe it didn't find it
                if (!translatedVi || translatedVi.toLowerCase() === word.toLowerCase()) {
                    translatedVi = isVietnamese ? word : "Không tìm thấy nghĩa rõ ràng.";
                }

                const dictRes = await fetch(`https://api.dictionaryapi.dev/api/v2/entries/en/${encodeURIComponent(searchWordEn)}`);
                
                if (dictRes.ok) {
                    const dictData = await dictRes.json();
                    const entry = dictData[0];
                    const ipa = entry.phonetics?.find(p => p.text)?.text || entry.phonetic || '';
                    
                    document.getElementById('dictWord').textContent = entry.word;
                    document.getElementById('dictIpa').textContent = ipa;
                    document.getElementById('dictMeaning').textContent = translatedVi;
                    
                    let meaningsHtml = '';
                    entry.meanings.forEach(m => {
                        meaningsHtml += `<div class="mb-4">
                            <h5 class="fw-bold text-secondary text-capitalize border-bottom pb-2 mb-3"><i class="fas fa-tag me-2"></i>${m.partOfSpeech}</h5>`;
                        
                        m.definitions.slice(0, 3).forEach((d, idx) => {
                            meaningsHtml += `<div class="mb-3">
                                <div class="fw-semibold"><span class="text-primary me-2">${idx + 1}.</span>${d.definition}</div>`;
                            if (d.example) {
                                meaningsHtml += `<div class="text-muted fst-italic mt-1 ms-4 border-start border-2 border-primary ps-2">"${d.example}"</div>`;
                            }
                            meaningsHtml += `</div>`;
                        });

                        if (m.synonyms && m.synonyms.length > 0) {
                            meaningsHtml += `<div class="mt-2 ms-4 mb-2"><strong class="text-dark">Đồng nghĩa: </strong>`;
                            meaningsHtml += m.synonyms.slice(0, 5).map(s => `<span class="badge bg-light text-dark border me-1">${s}</span>`).join('');
                            meaningsHtml += `</div>`;
                        }
                        
                        meaningsHtml += `</div>`;
                    });
                    
                    document.getElementById('dictMeaningsContainer').innerHTML = meaningsHtml;
                } else {
                    // Fallback to just translation
                    document.getElementById('dictWord').textContent = searchWordEn;
                    document.getElementById('dictIpa').textContent = '';
                    document.getElementById('dictMeaning').textContent = translatedVi;
                    document.getElementById('dictMeaningsContainer').innerHTML = '<div class="text-muted fst-italic">Chỉ tìm thấy bản dịch, không có dữ liệu từ điển chi tiết.</div>';
                }

                document.getElementById('dictLoading').classList.add('d-none');
                document.getElementById('dictResult').classList.remove('d-none');

            } catch (error) {
                document.getElementById('dictLoading').classList.add('d-none');
                document.getElementById('dictErrorText').textContent = error.message || 'Đã có lỗi xảy ra.';
                document.getElementById('dictError').classList.remove('d-none');
            } finally {
                document.getElementById('dictSubmitBtn').disabled = false;
            }
        });
    }

    // Dictionary resize logic
    const resizeHandle = document.querySelector('.resize-handle');
    let isResizing = false;
    let currentWidth = parseInt(localStorage.getItem('dictWidth')) || 500;

    if (resizeHandle && dictOffcanvasEl) {
        dictOffcanvasEl.style.width = currentWidth + 'px';

        resizeHandle.addEventListener('mousedown', function(e) {
            isResizing = true;
            resizeHandle.classList.add('active');
            document.body.style.cursor = 'ew-resize';
            dictOffcanvasEl.style.transition = 'none';
        });

        document.addEventListener('mousemove', function(e) {
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

        document.addEventListener('mouseup', function(e) {
            if (isResizing) {
                isResizing = false;
                resizeHandle.classList.remove('active');
                document.body.style.cursor = '';
                dictOffcanvasEl.style.transition = '';
                localStorage.setItem('dictWidth', currentWidth);
            }
        });
    }

    // Open dictionary from pinned tab
    document.querySelectorAll('.pinned-tab a[href="#"]').forEach(a => {
        a.addEventListener('click', function(e) {
            e.preventDefault();
            if (bsOffcanvas) {
                bsOffcanvas.show();
            }
        });
    });

    // Auto open dictionary if pinned on server
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

    // Save Word to Set Logic
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
            dictSaveWordBtn.addEventListener('click', async function() {
                const setId = dictSaveSetSelect.value;
                if (!setId) {
                    dictSaveMsg.className = 'small mt-2 text-danger';
                    dictSaveMsg.textContent = 'Vui lòng chọn một bộ từ!';
                    dictSaveMsg.classList.remove('d-none');
                    return;
                }

                dictSaveMsg.classList.add('d-none');
                dictSaveWordBtn.disabled = true;

                const term = document.getElementById('dictWord').textContent;
                const meaning = document.getElementById('dictMeaning').textContent;
                const ipaText = document.getElementById('dictIpa').textContent;
                const ipa = ipaText ? ipaText.replace(/\//g, '') : '';
                
                const examplesEl = document.getElementById('dictMeaningsContainer').querySelector('.fst-italic');
                let example = '';
                if (examplesEl) {
                    example = examplesEl.textContent.replace(/"/g, '');
                }

                const payload = {
                    SetId: parseInt(setId),
                    Term: term,
                    Meaning: meaning,
                    Ipa: ipa,
                    Example: example
                };

                try {
                    const res = await fetch('/api/VocabularyCard/CreateAjax', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify(payload)
                    });

                    if (res.ok) {
                        dictSaveMsg.className = 'small mt-2 text-success fw-bold';
                        dictSaveMsg.innerHTML = '<i class="fas fa-check-circle me-1"></i>Đã lưu thành công!';
                        dictSaveMsg.classList.remove('d-none');
                        setTimeout(() => dictSaveMsg.classList.add('d-none'), 3000);
                    } else {
                        throw new Error('Lỗi lưu từ');
                    }
                } catch (e) {
                    dictSaveMsg.className = 'small mt-2 text-danger';
                    dictSaveMsg.textContent = 'Có lỗi xảy ra, vui lòng thử lại.';
                    dictSaveMsg.classList.remove('d-none');
                } finally {
                    dictSaveWordBtn.disabled = false;
                }
            });
        }
    }
});
