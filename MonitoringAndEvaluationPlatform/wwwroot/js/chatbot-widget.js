$(function () {
    var $panel = $('#chatbot-panel');
    var $toggleBtn = $('#chatbot-toggle-btn');
    var $closeBtn = $('#chatbot-close-btn');
    var $input = $('#chatbot-input');
    var $sendBtn = $('#chatbot-send-btn');
    var $messages = $('#chatbot-messages');
    var $typing = $('#chatbot-typing');
    var locale = $panel.data('locale') || 'ar';
    var conversationHistory = [];
    var isStreaming = false;

    // Toggle panel
    $toggleBtn.on('click', function () {
        $panel.toggleClass('open');
        if ($panel.hasClass('open')) {
            $input.focus();
        }
    });

    $closeBtn.on('click', function () {
        $panel.removeClass('open');
    });

    // Send message
    $sendBtn.on('click', sendMessage);
    $input.on('keydown', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });

    function sendMessage() {
        var text = $input.val().trim();
        if (!text || isStreaming) return;

        // Add user message
        appendMessage('user', text);
        conversationHistory.push({ role: 'user', content: text });
        $input.val('');

        // Show typing indicator
        isStreaming = true;
        $sendBtn.prop('disabled', true);
        $typing.addClass('visible');
        scrollToBottom();

        // Stream response
        var assistantText = '';
        var $assistantBubble = null;

        fetch('/api/chatbot/stream', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'same-origin',
            body: JSON.stringify({
                messages: conversationHistory,
                locale: locale
            })
        }).then(function (response) {
            if (!response.ok) throw new Error('HTTP ' + response.status);
            var reader = response.body.getReader();
            var decoder = new TextDecoder();
            var buffer = '';

            // Hide typing, create assistant bubble
            $typing.removeClass('visible');
            $assistantBubble = $('<div class="chatbot-message assistant"></div>');
            $messages.append($assistantBubble);

            function read() {
                return reader.read().then(function (result) {
                    if (result.done) {
                        finishStream();
                        return;
                    }

                    buffer += decoder.decode(result.value, { stream: true });
                    var lines = buffer.split('\n');
                    buffer = lines.pop(); // Keep incomplete line

                    for (var i = 0; i < lines.length; i++) {
                        var line = lines[i].trim();
                        if (!line.startsWith('data: ')) continue;
                        var data = line.substring(6);
                        if (data === '[DONE]') {
                            finishStream();
                            return;
                        }
                        try {
                            var parsed = JSON.parse(data);
                            if (parsed.token) {
                                assistantText += parsed.token;
                                $assistantBubble.text(assistantText);
                                scrollToBottom();
                            }
                        } catch (e) {
                            // Skip malformed JSON
                        }
                    }

                    return read();
                });
            }

            return read();
        }).catch(function (err) {
            console.error('Chatbot error:', err);
            $typing.removeClass('visible');
            if (!$assistantBubble) {
                $assistantBubble = $('<div class="chatbot-message assistant"></div>');
                $messages.append($assistantBubble);
            }
            var errorMsg = locale === 'ar' ? 'حدث خطأ. يرجى المحاولة مرة أخرى.' :
                           locale === 'fr' ? 'Une erreur est survenue. Veuillez réessayer.' :
                           'An error occurred. Please try again.';
            $assistantBubble.text(errorMsg + ' (' + err.message + ')');
            scrollToBottom();
            finishStream();
        });

        function finishStream() {
            if (assistantText) {
                conversationHistory.push({ role: 'assistant', content: assistantText });
            }
            isStreaming = false;
            $sendBtn.prop('disabled', false);
            $typing.removeClass('visible');
        }
    }

    function appendMessage(role, text) {
        var $msg = $('<div class="chatbot-message"></div>').addClass(role).text(text);
        $messages.append($msg);
        scrollToBottom();
    }

    function scrollToBottom() {
        $messages.scrollTop($messages[0].scrollHeight);
    }
});
