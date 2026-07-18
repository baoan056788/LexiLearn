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
    const dictFab = document.querySelector('.dict-fab');
    let bsOffcanvas = null;

    function setDictFabVisible(isVisible) {
        if (!dictFab) return;
        dictFab.style.display = isVisible ? 'flex' : 'none';
    }

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

    // ── Vertical panel splitter (resize 2 panels inside offcanvas) ──
    (function initPanelSplitter() {
        const splitter = document.getElementById('panelSplitter');
        const splitContainer = document.getElementById('offcanvasSplitContainer');
        const dictPanel = document.getElementById('dictPanel');
        const aiPanel = document.getElementById('aiPanel');

        if (!splitter || !splitContainer || !dictPanel || !aiPanel) return;

        const STORAGE_KEY = 'dictPanelRatio';
        const MIN_HEIGHT = 80; // px minimum for each panel

        function applyRatio(ratio) {
            // ratio = fraction of container height for top panel (dictPanel)
            ratio = Math.max(0.1, Math.min(0.9, ratio));
            const containerH = splitContainer.clientHeight;
            const splitterH = splitter.offsetHeight;
            const available = containerH - splitterH;
            const topH = Math.max(MIN_HEIGHT, Math.min(available - MIN_HEIGHT, Math.round(available * ratio)));
            const botH = available - topH;
            dictPanel.style.flex = 'none';
            dictPanel.style.height = topH + 'px';
            aiPanel.style.flex = 'none';
            aiPanel.style.height = botH + 'px';
        }

        // Restore saved ratio
        const savedRatio = parseFloat(localStorage.getItem(STORAGE_KEY)) || 0.45;
        requestAnimationFrame(function () {
            applyRatio(savedRatio);
        });
        // Wait until offcanvas is shown to measure height
        if (dictOffcanvasEl) {
            dictOffcanvasEl.addEventListener('shown.bs.offcanvas', function onFirstShow() {
                applyRatio(savedRatio);
            });
        }
        // Also apply immediately if already open
        if (dictOffcanvasEl && dictOffcanvasEl.classList.contains('show')) {
            applyRatio(savedRatio);
        }

        let isDraggingPanel = false;
        let dragStartY = 0;
        let dragStartTopH = 0;

        splitter.addEventListener('mousedown', function (e) {
            e.preventDefault();
            isDraggingPanel = true;
            dragStartY = e.clientY;
            dragStartTopH = dictPanel.offsetHeight;
            splitter.classList.add('dragging');
            document.body.style.cursor = 'ns-resize';
            document.body.style.userSelect = 'none';
        });

        // Touch support
        splitter.addEventListener('touchstart', function (e) {
            isDraggingPanel = true;
            dragStartY = e.touches[0].clientY;
            dragStartTopH = dictPanel.offsetHeight;
            splitter.classList.add('dragging');
            document.body.style.userSelect = 'none';
        }, { passive: true });

        document.addEventListener('mousemove', function (e) {
            if (!isDraggingPanel) return;
            const delta = e.clientY - dragStartY;
            const containerH = splitContainer.clientHeight;
            const splitterH = splitter.offsetHeight;
            const available = containerH - splitterH;
            const newTopH = Math.max(MIN_HEIGHT, Math.min(available - MIN_HEIGHT, dragStartTopH + delta));
            const newBotH = available - newTopH;
            dictPanel.style.height = newTopH + 'px';
            aiPanel.style.height = newBotH + 'px';
        });

        document.addEventListener('touchmove', function (e) {
            if (!isDraggingPanel) return;
            const delta = e.touches[0].clientY - dragStartY;
            const containerH = splitContainer.clientHeight;
            const splitterH = splitter.offsetHeight;
            const available = containerH - splitterH;
            const newTopH = Math.max(MIN_HEIGHT, Math.min(available - MIN_HEIGHT, dragStartTopH + delta));
            const newBotH = available - newTopH;
            dictPanel.style.height = newTopH + 'px';
            aiPanel.style.height = newBotH + 'px';
        }, { passive: true });

        document.addEventListener('mouseup', function () {
            if (!isDraggingPanel) return;
            isDraggingPanel = false;
            splitter.classList.remove('dragging');
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
            // Save ratio
            const containerH = splitContainer.clientHeight;
            const splitterH = splitter.offsetHeight;
            const available = containerH - splitterH;
            const ratio = dictPanel.offsetHeight / available;
            localStorage.setItem(STORAGE_KEY, ratio.toFixed(4));
        });

        document.addEventListener('touchend', function () {
            if (!isDraggingPanel) return;
            isDraggingPanel = false;
            splitter.classList.remove('dragging');
            document.body.style.userSelect = '';
            const containerH = splitContainer.clientHeight;
            const splitterH = splitter.offsetHeight;
            const available = containerH - splitterH;
            const ratio = dictPanel.offsetHeight / available;
            localStorage.setItem(STORAGE_KEY, ratio.toFixed(4));
        });

        // Re-apply on window resize
        window.addEventListener('resize', function () {
            if (!dictOffcanvasEl || !dictOffcanvasEl.classList.contains('show')) return;
            const containerH = splitContainer.clientHeight;
            const splitterH = splitter.offsetHeight;
            const available = containerH - splitterH;
            const ratio = dictPanel.offsetHeight / available;
            applyRatio(ratio);
        });
    })();

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
        dictFab?.classList.add('offcanvas-open');
        document.body.classList.add('dict-offcanvas-open');
        setDictFabVisible(false);

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
            dictFab?.classList.remove('offcanvas-open');
            document.body.classList.remove('dict-offcanvas-open');
            setDictFabVisible(true);
        });

        dictOffcanvasEl.addEventListener('shown.bs.offcanvas', function () {
            if (document.getElementById('pinned-dict-tab')) {
                document.body.style.marginRight = currentWidth + 'px';
            }
            dictFab?.classList.add('offcanvas-open');
            document.body.classList.add('dict-offcanvas-open');
            setDictFabVisible(false);
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

    const dictSpeakBtn = document.getElementById('dictSpeakBtn');
    if (dictSpeakBtn) {
        dictSpeakBtn.addEventListener('click', function () {
            const word = document.getElementById('dictWord')?.textContent?.trim();
            if (word) speakEnglish(word);
        });
    }

    const aiChatForm = document.getElementById('aiChatForm');
    const aiChatInput = document.getElementById('aiChatInput');
    const aiChatMessages = document.getElementById('aiChatMessages');
    const aiConversationSelect = document.getElementById('aiConversationSelect');
    const aiNewChatBtn = document.getElementById('aiNewChatBtn');
    const aiDeleteChatBtn = document.getElementById('aiDeleteChatBtn');
    const aiChatLoading = document.getElementById('aiChatLoading');
    const aiChatError = document.getElementById('aiChatError');
    const aiChatSendBtn = document.getElementById('aiChatSendBtn');
    let currentAiConversationId = null;

    if (aiChatForm && aiChatMessages) {
        rehydrateExistingAiMessages();
        loadAiConversations();

        aiConversationSelect?.addEventListener('change', async function () {
            const id = parseInt(aiConversationSelect.value, 10);
            if (!id) {
                startNewAiConversation();
                return;
            }

            currentAiConversationId = id;
            await loadAiConversation(id);
        });

        aiNewChatBtn?.addEventListener('click', startNewAiConversation);

        aiChatInput?.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                aiChatForm.requestSubmit();
            }
        });

        aiDeleteChatBtn?.addEventListener('click', async function () {
            if (!currentAiConversationId) return;

            aiDeleteChatBtn.disabled = true;
            clearAiError();

            try {
                const response = await fetch(`/api/ai/conversations/${currentAiConversationId}`, { method: 'DELETE' });
                if (!response.ok) throw new Error('Không xóa được cuộc trò chuyện.');
                startNewAiConversation();
                await loadAiConversations();
            } catch (error) {
                showAiError(error.message || 'Đã có lỗi xảy ra.');
            } finally {
                aiDeleteChatBtn.disabled = false;
            }
        });

        aiChatForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            const message = aiChatInput.value.trim();
            if (!message) return;

            appendAiMessage('user', message);
            aiChatInput.value = '';
            aiChatLoading?.classList.remove('d-none');
            clearAiError();
            if (aiChatSendBtn) aiChatSendBtn.disabled = true;

            try {
                const response = await fetch('/api/ai/chat', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        conversationId: currentAiConversationId,
                        message
                    })
                });

                const data = await response.json().catch(() => ({}));
                if (!response.ok) {
                    if (response.status === 401 || response.status === 403) {
                        throw new Error('Vui lòng đăng nhập để dùng AI.');
                    }

                    throw new Error(data.message || 'Gemini chưa trả lời được. Vui lòng thử lại.');
                }

                currentAiConversationId = data.conversationId;
                if (Array.isArray(data.messages) && data.messages.length > 0) {
                    renderAiMessages(data.messages);
                } else if (data.reply) {
                    appendAiMessage('assistant', data.reply);
                } else if (currentAiConversationId) {
                    await loadAiConversation(currentAiConversationId);
                } else {
                    throw new Error('AI chua tra ve noi dung hop le.');
                }
                await loadAiConversations(currentAiConversationId);
            } catch (error) {
                showAiError(error.message || 'Đã có lỗi xảy ra.');
            } finally {
                aiChatLoading?.classList.add('d-none');
                if (aiChatSendBtn) aiChatSendBtn.disabled = false;
            }
        });
    }

    const noteForm = document.getElementById('noteForm');
    const noteIdInput = document.getElementById('noteId');
    const noteTitleInput = document.getElementById('noteTitleInput');
    const noteRelatedTermInput = document.getElementById('noteRelatedTermInput');
    const noteTagsInput = document.getElementById('noteTagsInput');
    const noteContentInput = document.getElementById('noteContentInput');
    const noteSetSelect = document.getElementById('noteSetSelect');
    const noteList = document.getElementById('noteList');
    const noteError = document.getElementById('noteError');
    const noteSaveMsg = document.getElementById('noteSaveMsg');
    const noteSearchInput = document.getElementById('noteSearchInput');
    const noteSearchBtn = document.getElementById('noteSearchBtn');
    const noteNewBtn = document.getElementById('noteNewBtn');
    const notePinBtn = document.getElementById('notePinBtn');
    const noteDeleteBtn = document.getElementById('noteDeleteBtn');
    const noteUseWordBtn = document.getElementById('noteUseWordBtn');
    const noteUseAiBtn = document.getElementById('noteUseAiBtn');
    const noteSaveBtn = document.getElementById('noteSaveBtn');
    const noteDraftStorageKey = 'lexilearn.noteDraft.v1';
    let currentNotePinned = false;
    let workspaceQuill = null;

    if (noteForm) {
        if (document.getElementById('workspaceQuillContainer')) {
            try {
                workspaceQuill = new Quill('#workspaceQuillContainer', {
                    theme: 'snow',
                    placeholder: 'Ghi chu, meo nho, vi du, loi hay nham...',
                    modules: {
                        toolbar: [
                            ['bold', 'italic', 'underline', 'strike'],
                            [{ 'list': 'ordered'}, { 'list': 'bullet' }],
                            [{ 'color': [] }, { 'background': [] }],
                            ['clean']
                        ]
                    }
                });
                workspaceQuill.on('text-change', function() {
                    if (noteContentInput) {
                        noteContentInput.value = workspaceQuill.root.innerHTML;
                        persistNoteDraft();
                    }
                });
            } catch (e) {
                console.error('Quill initialization failed:', e);
                workspaceQuill = null;
            }
        }

        loadNotebookSetOptions();
        loadNotes();
        updateNotePinButton();
        restoreNoteDraft();

        noteTitleInput?.addEventListener('input', persistNoteDraft);
        noteRelatedTermInput?.addEventListener('input', persistNoteDraft);
        noteTagsInput?.addEventListener('input', persistNoteDraft);
        noteContentInput?.addEventListener('input', persistNoteDraft);
        noteSetSelect?.addEventListener('change', persistNoteDraft);

        noteSearchBtn?.addEventListener('click', function () {
            loadNotes(noteSearchInput?.value?.trim() || '');
        });

        noteSearchInput?.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                loadNotes(noteSearchInput.value.trim());
            }
        });

        noteNewBtn?.addEventListener('click', function () {
            resetNoteForm();
            hideNoteMessages();
        });

        noteForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            hideNoteMessages();

            const payload = {
                studyNoteId: noteIdInput.value ? parseInt(noteIdInput.value, 10) : null,
                setId: noteSetSelect?.value ? parseInt(noteSetSelect.value, 10) : null,
                title: noteTitleInput.value.trim(),
                relatedTerm: noteRelatedTermInput.value.trim(),
                tags: noteTagsInput.value.trim(),
                content: noteContentInput.value.trim(),
                isPinned: currentNotePinned
            };

            try {
                const response = await fetch('/api/notebook', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });
                const data = await response.json().catch(() => ({}));
                if (!response.ok) {
                    throw new Error(data.message || 'Khong luu duoc note.');
                }

                noteIdInput.value = data.studyNoteId || '';
                showNoteSaveMessage('Da luu note.');
                clearNoteDraft();
                await loadNoteDetail(data.studyNoteId);
                await loadNotes(noteSearchInput?.value?.trim() || '', data.studyNoteId);
            } catch (error) {
                showNoteError(error.message || 'Da co loi xay ra.');
            }
        });

        noteDeleteBtn?.addEventListener('click', async function () {
            const noteId = parseInt(noteIdInput.value || '0', 10);
            if (!noteId) {
                showNoteError('Hay chon mot note de xoa.');
                return;
            }

            hideNoteMessages();
            try {
                const response = await fetch(`/api/notebook/${noteId}`, { method: 'DELETE' });
                const data = await response.json().catch(() => ({}));
                if (!response.ok) {
                    throw new Error(data.message || 'Khong xoa duoc note.');
                }

                resetNoteForm();
                showNoteSaveMessage('Da xoa note.');
                await loadNotes(noteSearchInput?.value?.trim() || '');
            } catch (error) {
                showNoteError(error.message || 'Da co loi xay ra.');
            }
        });

        notePinBtn?.addEventListener('click', async function () {
            const noteId = parseInt(noteIdInput.value || '0', 10);
            if (!noteId) {
                currentNotePinned = !currentNotePinned;
                updateNotePinButton();
                return;
            }

            hideNoteMessages();
            try {
                const response = await fetch(`/api/notebook/${noteId}/pin`, { method: 'POST' });
                const data = await response.json().catch(() => ({}));
                if (!response.ok) {
                    throw new Error(data.message || 'Khong doi duoc trang thai ghim.');
                }

                currentNotePinned = !!data.isPinned;
                updateNotePinButton();
                await loadNotes(noteSearchInput?.value?.trim() || '', noteId);
            } catch (error) {
                showNoteError(error.message || 'Da co loi xay ra.');
            }
        });

        noteUseWordBtn?.addEventListener('click', function () {
            const currentWord = document.getElementById('dictWord')?.textContent?.trim() || '';
            const currentMeaning = document.getElementById('dictMeaning')?.textContent?.trim() || '';
            const currentIpa = document.getElementById('dictIpa')?.textContent?.trim() || '';
            if (!currentWord) {
                showNoteError('Chua co tu dang tra de chen vao note.');
                return;
            }

            if (!noteTitleInput.value.trim()) {
                noteTitleInput.value = `Ghi chu: ${currentWord}`;
            }
            if (!noteRelatedTermInput.value.trim()) {
                noteRelatedTermInput.value = currentWord;
            }

            const chunk = [`Tu: ${currentWord}`];
            if (currentMeaning) chunk.push(`Nghia: ${currentMeaning}`);
            if (currentIpa) chunk.push(`IPA: ${currentIpa}`);
            appendNoteContent(chunk.join('\n'));
        });

        noteUseAiBtn?.addEventListener('click', function () {
            const lastAssistantBubble = aiChatMessages?.querySelector('.ai-message.assistant:last-child .ai-bubble');
            const aiText = lastAssistantBubble ? lastAssistantBubble.textContent.trim() : '';
            if (!aiText) {
                showNoteError('Chua co cau tra loi AI de dua vao note.');
                return;
            }

            appendNoteContent(`Tom tat tu AI:\n${aiText}`);
        });
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

        const hasBodyContent =
            ((data.definitions && data.definitions.length > 0) ||
            (data.synonyms && data.synonyms.length > 0) ||
            (data.antonyms && data.antonyms.length > 0) ||
            data.note);

        if (!hasBodyContent) {
            meaningsHtml += `<div class="text-muted small">Chua co thong tin chi tiet tu AI cho muc nay.</div>`;
        }

        meaningsHtml += '</div>';
        document.getElementById('dictMeaningsContainer').innerHTML = meaningsHtml;
    }

    function showSaveMessage(message, className, success = false) {
        dictSaveMsg.className = `small mt-2 ${className}`;
        dictSaveMsg.innerHTML = success ? `<i class="fas fa-check-circle me-1"></i>${message}` : message;
        dictSaveMsg.classList.remove('d-none');
    }

    async function loadAiConversations(selectedId = currentAiConversationId) {
        if (!aiConversationSelect) return;

        try {
            const response = await fetch('/api/ai/conversations');
            if (!response.ok) return;

            const conversations = await response.json();
            aiConversationSelect.innerHTML = '<option value="">Cuộc trò chuyện mới</option>';
            conversations.forEach(conversation => {
                const option = document.createElement('option');
                option.value = conversation.conversationId;
                option.textContent = conversation.title || `Cuộc trò chuyện #${conversation.conversationId}`;
                aiConversationSelect.appendChild(option);
            });

            if (selectedId) {
                aiConversationSelect.value = String(selectedId);
            }
        } catch {
            // The chat panel stays usable for a new conversation even if history cannot load.
        }
    }

    async function loadAiConversation(id) {
        clearAiError();
        try {
            const response = await fetch(`/api/ai/conversations/${id}`);
            const data = await response.json().catch(() => ({}));
            if (!response.ok) throw new Error(data.message || 'Không tải được lịch sử.');
            renderAiMessages(data.messages || []);
        } catch (error) {
            showAiError(error.message || 'Đã có lỗi xảy ra.');
        }
    }

    function startNewAiConversation() {
        currentAiConversationId = null;
        if (aiConversationSelect) aiConversationSelect.value = '';
        renderAiMessages([]);
        clearAiError();
        aiChatInput?.focus();
    }

    function renderAiMessages(messages) {
        if (!aiChatMessages) return;

        aiChatMessages.innerHTML = '';

        if (!messages.length) {
            aiChatMessages.innerHTML = `<div class="ai-empty-state text-center text-muted py-4">
                <i class="fas fa-wand-magic-sparkles fs-2 mb-3 text-primary"></i>
                <p class="mb-1 fw-semibold">Hỏi AI về từ vựng, ví dụ, ngữ pháp.</p>
                <p class="small mb-0">Lịch sử sẽ được lưu riêng cho tài khoản của bạn.</p>
            </div>`;
            return;
        }

        messages.forEach(message => appendAiMessage(message.role, message.content));
        aiChatMessages.scrollTop = aiChatMessages.scrollHeight;
    }

    function appendAiMessage(role, content) {
        if (!aiChatMessages) return;

        const emptyState = aiChatMessages.querySelector('.ai-empty-state');
        if (emptyState) emptyState.remove();

        const wrapper = document.createElement('div');
        wrapper.className = `ai-message ${role === 'assistant' ? 'assistant' : 'user'}`;

        const bubble = document.createElement('div');
        bubble.className = 'ai-bubble';
        if (role === 'assistant') {
            bubble.innerHTML = formatAiMessageHtml(content || '');
        } else {
            bubble.textContent = content || '';
        }
        wrapper.appendChild(bubble);

        if (role === 'assistant') {
            const words = extractEnglishWords(content).slice(0, 6);
            if (words.length > 0) {
                const speakList = document.createElement('div');
                speakList.className = 'ai-speak-words';
                words.forEach(word => {
                    const button = document.createElement('button');
                    button.type = 'button';
                    button.className = 'btn btn-sm btn-outline-primary ai-speak-word';
                    button.innerHTML = `<i class="fas fa-volume-high me-1"></i>${escapeHtml(word)}`;
                    button.addEventListener('click', () => speakEnglish(word));
                    speakList.appendChild(button);
                });
                bubble.appendChild(speakList);
            }
        }

        aiChatMessages.appendChild(wrapper);
        aiChatMessages.scrollTop = aiChatMessages.scrollHeight;
    }

    function rehydrateExistingAiMessages() {
        if (!aiChatMessages) return;

        aiChatMessages.querySelectorAll('.ai-message.assistant .ai-bubble').forEach(function (bubble) {
            if (bubble.dataset.formatted === 'true') return;

            const speakList = bubble.querySelector('.ai-speak-words');
            const speakClone = speakList ? speakList.cloneNode(true) : null;
            if (speakList) {
                speakList.remove();
            }

            const rawText = bubble.textContent || '';
            bubble.innerHTML = formatAiMessageHtml(rawText);
            bubble.dataset.formatted = 'true';

            if (speakClone) {
                bubble.appendChild(speakClone);
            }
        });
    }

    function extractEnglishWords(text) {
        const stopWords = new Set([
            'the', 'and', 'for', 'you', 'are', 'with', 'this', 'that', 'from', 'have', 'will',
            'trong', 'anh', 'nghia', 'dich', 'vi', 'du', 'ipa', 'cach', 'noi', 'yeu', 'bang', 'tieng'
        ]);
        const preferredPool = [
            ...matchAllGroups(String(text || ''), /"([^"]+)"/g),
            ...matchAllGroups(String(text || ''), /^#{1,6}\s+(.+)$/gm)
        ].join(' ');
        const sourceText = preferredPool || stripMarkdownArtifacts(String(text || ''));
        const matches = sourceText.match(/\b[a-zA-Z][a-zA-Z'-]{2,}\b/g) || [];
        return [...new Set(matches.map(word => word.toLowerCase()))]
            .filter(word => !stopWords.has(word))
            .slice(0, 12);
    }

    function formatAiMessageHtml(rawText) {
        const lines = String(rawText || '').replace(/\r\n/g, '\n').split('\n');
        const fragments = [];
        let inList = false;

        const closeList = function () {
            if (inList) {
                fragments.push('</ul>');
                inList = false;
            }
        };

        lines.forEach(function (line) {
            const trimmed = line.trim();

            if (!trimmed) {
                closeList();
                return;
            }

            const headingMatch = trimmed.match(/^#{1,6}\s+(.+)$/);
            if (headingMatch) {
                closeList();
                fragments.push(`<h6>${formatInlineText(headingMatch[1])}</h6>`);
                return;
            }

            const bulletMatch = trimmed.match(/^[-*]\s+(.+)$/);
            if (bulletMatch) {
                if (!inList) {
                    fragments.push('<ul>');
                    inList = true;
                }
                fragments.push(`<li>${formatInlineText(bulletMatch[1])}</li>`);
                return;
            }

            const numberedMatch = trimmed.match(/^\d+\.\s+(.+)$/);
            if (numberedMatch) {
                closeList();
                fragments.push(`<p class="ai-numbered">${formatInlineText(numberedMatch[1])}</p>`);
                return;
            }

            closeList();
            fragments.push(`<p>${formatInlineText(trimmed)}</p>`);
        });

        closeList();
        return fragments.join('');
    }

    function formatInlineText(value) {
        let html = escapeHtml(value);
        html = html.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
        html = html.replace(/__([^_]+)__/g, '<strong>$1</strong>');
        html = html.replace(/\*([^*]+)\*/g, '<em>$1</em>');
        html = html.replace(/`([^`]+)`/g, '<code>$1</code>');
        html = html.replace(/(^|\s)#{1,6}\s*/g, '$1');
        html = html.replace(/\*\*/g, '');
        return html;
    }

    function stripMarkdownArtifacts(value) {
        return String(value || '')
            .replace(/\r\n/g, '\n')
            .replace(/^#{1,6}\s*/gm, '')
            .replace(/\*\*/g, '')
            .replace(/__/g, '')
            .replace(/`/g, '')
            .replace(/^>\s?/gm, '');
    }

    function matchAllGroups(text, regex) {
        return Array.from(text.matchAll(regex), function (match) {
            return match[1] || '';
        });
    }

    function speakEnglish(text) {
        if (!('speechSynthesis' in window)) {
            return;
        }

        window.speechSynthesis.cancel();
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = 'en-US';
        utterance.rate = 0.9;
        window.speechSynthesis.speak(utterance);
    }

    // Expose globally so inline scripts in Views can call speakEnglish(...)
    window.speakEnglish = speakEnglish;

    function showAiError(message) {
        if (!aiChatError) return;
        aiChatError.textContent = message;
        aiChatError.classList.remove('d-none');
    }

    function clearAiError() {
        aiChatError?.classList.add('d-none');
    }

    async function loadNotebookSetOptions() {
        if (!noteSetSelect) return;
        try {
            const response = await fetch('/api/VocabularySet/MySets');
            if (!response.ok) return;

            const sets = await response.json();
            noteSetSelect.innerHTML = '<option value="">Khong gan bo tu</option>';
            sets.forEach(set => {
                const option = document.createElement('option');
                option.value = set.setId;
                option.textContent = set.title;
                noteSetSelect.appendChild(option);
            });

            try {
                const raw = localStorage.getItem(noteDraftStorageKey);
                if (raw) {
                    const draft = JSON.parse(raw);
                    if (draft.setId) {
                        noteSetSelect.value = draft.setId;
                    }
                }
            } catch {
                // Ignore invalid draft state.
            }
        } catch {
            // Ignore and keep the default option.
        }
    }

    async function loadNotes(search = '', selectedId = null) {
        if (!noteList) return;

        hideNoteMessages();
        try {
            const query = search ? `?search=${encodeURIComponent(search)}` : '';
            const response = await fetch(`/api/notebook${query}`);
            const data = await response.json().catch(() => ([]));
            if (!response.ok) {
                throw new Error(data.message || 'Khong tai duoc so tay.');
            }

            renderNoteList(data || [], selectedId);
        } catch (error) {
            showNoteError(error.message || 'Da co loi xay ra.');
        }
    }

    function renderNoteList(notes, selectedId = null) {
        if (!noteList) return;

        noteList.innerHTML = '';

        if (!notes.length) {
            noteList.innerHTML = '<div class="text-muted small">Chua co note nao.</div>';
            return;
        }

        notes.forEach(note => {
            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'note-item';
            if (selectedId && note.studyNoteId === selectedId) {
                button.classList.add('active');
            }
            button.innerHTML = `
                <div class="note-item-title">
                    <span>${escapeHtml(note.title || 'Note')}</span>
                    <span>${note.isPinned ? '<i class="fas fa-thumbtack text-primary"></i>' : ''}</span>
                </div>
                <div class="note-item-meta">${escapeHtml(note.relatedTerm || note.setTitle || '')}</div>
                <div class="note-item-preview">${escapeHtml(stripHtml(note.preview || ''))}</div>
            `;
            button.addEventListener('click', function () {
                loadNoteDetail(note.studyNoteId);
            });
            noteList.appendChild(button);
        });
    }

    async function loadNoteDetail(noteId) {
        hideNoteMessages();
        try {
            const response = await fetch(`/api/notebook/${noteId}`);
            const data = await response.json().catch(() => ({}));
            if (!response.ok) {
                throw new Error(data.message || 'Khong mo duoc note.');
            }

            noteIdInput.value = data.studyNoteId || '';
            noteTitleInput.value = data.title || '';
            noteRelatedTermInput.value = data.relatedTerm || '';
            noteTagsInput.value = data.tags || '';
            noteContentInput.value = data.content || '';
            if (workspaceQuill) workspaceQuill.root.innerHTML = data.content || '';
            if (noteSetSelect) {
                noteSetSelect.value = data.setId ? String(data.setId) : '';
            }
            currentNotePinned = !!data.isPinned;
            updateNotePinButton();
            clearNoteDraft();
            await loadNotes(noteSearchInput?.value?.trim() || '', data.studyNoteId);
        } catch (error) {
            showNoteError(error.message || 'Da co loi xay ra.');
        }
    }

    function resetNoteForm() {
        if (noteForm) noteForm.reset();
        if (noteIdInput) noteIdInput.value = '';
        if (workspaceQuill) workspaceQuill.root.innerHTML = '';
        currentNotePinned = false;
        updateNotePinButton();
        clearNoteDraft();
    }

    function updateNotePinButton() {
        if (!notePinBtn) return;
        notePinBtn.classList.toggle('btn-outline-secondary', !currentNotePinned);
        notePinBtn.classList.toggle('btn-primary', currentNotePinned);
        notePinBtn.innerHTML = currentNotePinned
            ? '<i class="fas fa-thumbtack me-1"></i>Da ghim'
            : '<i class="fas fa-thumbtack me-1"></i>Ghim';
    }

    function appendNoteContent(text) {
        if (!noteContentInput || !text) return;
        if (workspaceQuill) {
            const length = workspaceQuill.getLength();
            workspaceQuill.insertText(length ? length - 1 : 0, (length > 1 ? '\n' : '') + text);
            noteContentInput.value = workspaceQuill.root.innerHTML;
        } else {
            const current = noteContentInput.value.trim();
            noteContentInput.value = current ? `${current}\n\n${text}` : text;
        }
        persistNoteDraft();
    }

    function persistNoteDraft() {
        if (!noteForm) return;

        const draft = {
            studyNoteId: noteIdInput?.value || '',
            title: noteTitleInput?.value || '',
            relatedTerm: noteRelatedTermInput?.value || '',
            tags: noteTagsInput?.value || '',
            content: noteContentInput?.value || '',
            setId: noteSetSelect?.value || '',
            isPinned: currentNotePinned
        };

        const hasContent = Object.values(draft).some(value => value === true || (typeof value === 'string' && value.trim() !== ''));
        if (!hasContent) {
            localStorage.removeItem(noteDraftStorageKey);
            return;
        }

        localStorage.setItem(noteDraftStorageKey, JSON.stringify(draft));
    }

    function restoreNoteDraft() {
        if (!noteForm) return;

        try {
            const raw = localStorage.getItem(noteDraftStorageKey);
            if (!raw) return;

            const draft = JSON.parse(raw);
            if (noteIdInput) noteIdInput.value = draft.studyNoteId || '';
            if (noteTitleInput && !noteTitleInput.value.trim()) noteTitleInput.value = draft.title || '';
            if (noteRelatedTermInput && !noteRelatedTermInput.value.trim()) noteRelatedTermInput.value = draft.relatedTerm || '';
            if (noteTagsInput && !noteTagsInput.value.trim()) noteTagsInput.value = draft.tags || '';
            if (noteContentInput && !noteContentInput.value.trim()) {
                noteContentInput.value = draft.content || '';
                if (workspaceQuill) workspaceQuill.root.innerHTML = draft.content || '';
            }
            if (noteSetSelect && draft.setId) noteSetSelect.value = draft.setId;
            currentNotePinned = !!draft.isPinned;
            updateNotePinButton();
        } catch {
            localStorage.removeItem(noteDraftStorageKey);
        }
    }

    function clearNoteDraft() {
        localStorage.removeItem(noteDraftStorageKey);
    }

    function showNoteError(message) {
        if (!noteError) return;
        noteError.textContent = message;
        noteError.classList.remove('d-none');
        noteSaveMsg?.classList.add('d-none');
    }

    function showNoteSaveMessage(message) {
        if (!noteSaveMsg) return;
        noteSaveMsg.textContent = message;
        noteSaveMsg.classList.remove('d-none');
        noteError?.classList.add('d-none');
    }

    function hideNoteMessages() {
        noteError?.classList.add('d-none');
        noteSaveMsg?.classList.add('d-none');
    }

    function stripHtml(html) {
        let tmp = document.createElement("DIV");
        tmp.innerHTML = html || '';
        return tmp.textContent || tmp.innerText || "";
    }

    function escapeHtml(value) {
        return String(value ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    // --- Global SweetAlert2 confirm override ---
    document.querySelectorAll('[onsubmit*="return confirm("]').forEach(function (form) {
        const onsubmitContent = form.getAttribute('onsubmit');
        const match = onsubmitContent.match(/return\s+confirm\s*\(\s*['"](.*?)['"]\s*\)/);
        if (match && match[1]) {
            const message = match[1];
            form.removeAttribute('onsubmit');
            form.addEventListener('submit', function (e) {
                e.preventDefault();
                Swal.fire({
                    title: message,
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#d33',
                    cancelButtonColor: '#6c757d',
                    confirmButtonText: 'Đồng ý',
                    cancelButtonText: 'Hủy'
                }).then((result) => {
                    if (result.isConfirmed) {
                        form.submit();
                    }
                });
            });
        }
    });

    document.querySelectorAll('[onclick*="return confirm("]').forEach(function (btn) {
        const onclickContent = btn.getAttribute('onclick');
        const match = onclickContent.match(/return\s+confirm\s*\(\s*['"](.*?)['"]\s*\)/);
        if (match && match[1]) {
            const message = match[1];
            btn.removeAttribute('onclick');
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                Swal.fire({
                    title: message,
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#3085d6',
                    cancelButtonColor: '#6c757d',
                    confirmButtonText: 'Đồng ý',
                    cancelButtonText: 'Hủy'
                }).then((result) => {
                    if (result.isConfirmed) {
                        if (btn.type === 'submit' && btn.closest('form')) {
                            btn.closest('form').submit();
                        } else if (btn.tagName === 'A' && btn.href) {
                            window.location.href = btn.href;
                        }
                    }
                });
            });
        }
    });
});
