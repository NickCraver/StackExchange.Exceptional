$(function () {
    var table = $('.js-error-list'), lastSelected, Actions = {
        Protect: 'protect',
        Delete: 'delete'
    };
    hljs.initHighlighting();

    $(document).delegate('a.js-show-details', 'click', function (e) {
        e.preventDefault();
        $(this).text($(this).text() === 'view details' ? 'hide details' : 'view details')
            .parent().siblings('.details').toggle();
    });

    if (table.length === 0) {
        return;
    }

    // update title on main error log page to show count
    document.title = $('.js-error-count').text();

    // give 'em the finger
    table.find('th').css('cursor', 'pointer');

    // allow sorting
    table.tablesorter({
        sortList: [[5, 1]],
        textExtraction: {
            5: function (node, table, cellIndex) { return $(node).attr('title'); }
        }
    });
    // Delete link
    table.delegate('a.js-delete-link', 'click', function (e) {
        e.preventDefault();
        var jThis = $(this);

        // if we've "protected" this error, confirm the deletion
        if (jThis.closest('tr.js-protected').length && !e.ctrlKey && !confirm('Really delete this protected error?')) return false;

        var jRow = jThis.closest('tr'),
            jCell = jThis.closest('td').addClass('loading');

        $.post(baseUrl + Actions.Delete, { guid: jRow.data('id') })
            .done(function () {
                jRow.find('td').fadeOut(50, function () { $(this).closest('tr').remove(); });
                table.trigger('update');
            })
            .fail(function () {
                jCell.removeClass('loading');
                alert('Error occurred when trying to delete');
            });
    });
    // Protection link
    table.delegate('a.js-protect-link', 'click', function (e) {
        e.preventDefault();
        var jThis = $(this),
            jRow = jThis.closest('tr'),
            jCell = jThis.closest('td').addClass('loading');

        $.post(baseUrl + Actions.Protect, { guid: jRow.data('id') })
            .done(function () {
                var span = $("<span title=\"This error is protected\" />").append(jThis.children());
                jThis.replaceWith(span);
                jRow.addClass('js-protected');
            })
            .fail(function () {
                alert('An error occurred while trying to protect this exception.');
            })
            .always(function () {
                jCell.removeClass('loading');
            });
    });
    // allow range selection
    table.delegate('td', 'click', function (e) {
        if ($(e.target).closest('a').length) {
            return;
        }

        var row = $(this).closest('tr').toggleClass('selected');

        if (e.shiftKey) {
            var index = row.index(),
                lastIndex = lastSelected.index();
            if (!e.ctrlKey) {
                row.siblings().addBack().removeClass('selected');
            }
            row.parent()
                .children()
                .slice(Math.min(index, lastIndex), Math.max(index, lastIndex)).add(lastSelected).add(row)
                .addClass('selected');
            if (!e.ctrlKey) {
                lastSelected = row.first();
            }
            window.getSelection().removeAllRanges();
        } else if (e.ctrlKey) {
            lastSelected = row.first();
        } else {
            if ($('.exceptions-dashboard tbody td').length > 2) {
                row.addClass('selected');
            }
            row.siblings().removeClass('selected');
            lastSelected = row.first();
        }
    });
    // allow protection and deletion of range selection
    $(document).keyup(function (e) {
        var action, selected = $('.error.selected').not('.js-protected');

        if (selected.length === 0) {
            return;
        }

        switch (e.keyCode) {
            case 46: // del = delete
            case 68: // d = delete
                action = Actions.Delete;
                break;
            case 80: // p = protect
                action = Actions.Protect;
                break;
            default:
                return;
        }

        var ids = selected.map(function () { return $(this).data('id'); }).get();
        selected.children('td:first-child').addClass('loading');

        $.ajax({
            type: 'POST',
            traditional: true,
            data: { ids: ids },
            dataType: 'json',
            url: baseUrl + action + '-list'
        }).done(function (data) {
            if (!data.result) {
                alert('Error occurred when trying to ' + action + ' these errors.');
                return;
            }
            switch (action) {
                case Actions.Delete:
                    selected.remove();
                    table.trigger('update', [true]);
                    break;
                case Actions.Protect:
                    selected.addClass('js-protected').find('.js-protect-link').remove();
                    break;
            }
        }).fail(function (a, b, c) {
            console.log(a, b, c);
            selected.children('.loading').removeClass('loading');
            alert('Error occurred when trying to ' + action + ' these errors.');
        });
    }).delegate('a.js-clear-all', 'click', function (e) {
        e.preventDefault();

        if (!e.ctrlKey && !confirm('Really delete all non-protected errors?')) return false;

        var loading = $('.error-list tr:not(.js-protected) td:nth-child(1)').addClass('loading');

        $.post(baseUrl + 'delete-all')
            .done(function () {
                window.location.reload(true);
            })
            .fail(function () {
                loading.removeClass('loading');
                alert('Error occurred when trying to delete all errors.');
            });
    });
});