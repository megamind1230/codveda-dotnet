document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.column-card-list').forEach(function (el) {
        new Sortable(el, {
            group: 'kanban',
            animation: 200,
            easing: 'cubic-bezier(1, 0, 0, 1)',
            forceFallback: true,
            fallbackClass: 'sortable-fallback',
            fallbackOnBody: true,
            onStart: function () {
                document.body.style.userSelect = 'none';
            },
            onEnd: function (evt) {
                document.body.style.userSelect = '';
                var cardId = evt.item.dataset.cardId;
                var toColumnId = evt.to.closest('.column').dataset.columnId;
                var newOrder = Array.from(evt.to.children).indexOf(evt.item);

                fetch('/api/cards/' + cardId + '/move', {
                    method: 'PATCH',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        targetColumnId: parseInt(toColumnId),
                        newOrder: newOrder
                    })
                });
            }
        });
    });
});
