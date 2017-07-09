$(function () {
    var table = $('#ErrorLog'), lastSelected, Actions = {
        Protect: 'protect',
        Delete: 'delete'
    };
    prettyPrint();

    if (table.length == 0) {
        return;
    }

    // make columns sortable    
    // we need a special sorter for error creation date - the raw datetime is stored in a row's Time cell's title
    $.tablesorter.addParser({
        id: 'errorDate',
        is: function () { return false; },
        format: function (s, t, cell) {
            var date = $(cell).find('span').attr('title'); // e.g. 2011-03-31 01:57:59Z
            if (!date) return 0;

            var exp = /(\d{4})-(\d{1,2})-(\d{1,2})\W*(\d{1,2}):(\d{1,2}):(\d{1,2})Z/i.exec(date);
            return new Date(exp[1], exp[2] - 1, exp[3], exp[4], exp[5], exp[6], 0).getTime();
        },
        type: 'numeric'
    });

    // give 'em the finger
    table.find('th').css('cursor', 'pointer');

    // allow sorting
    table.tablesorter({
        headers: {
            5: { sorter: 'errorDate' }
        },
        sortList: [[5, 1]]
    });

    // update title on main error log page to show count
    document.title = $('#errorcount').text();

    table.delegate('a.js-delete-link', 'click', function (e) {
        e.preventDefault();
        var jThis = $(this);

        // if we've "protected" this error, confirm the deletion
        if (jThis.closest('tr.protected').length && !confirm('Really delete this protected error?')) return false;

        var url = jThis.data('url'),
            jRow = jThis.closest('tr'),
            jCell = jThis.closest('td').addClass('loading');

        $.ajax({
            type: 'POST',
            data: { guid: jRow.data('id') },
            context: this,
            url: url,
            success: function () {
                jRow.find('td').fadeOut('fast', function() { $(this).closest('tr').remove(); });
                table.trigger("update");
            },
            error: function () {
                jCell.removeClass('loading');
                alert('Error occurred when trying to delete');
            }
        });
        return false;
    });
    table.delegate('a.js-protect-link', 'click', function (e) {
        e.preventDefault();
        var url = $(this).data('url'),
            jRow = $(this).closest('tr'),
            jCell = $(this).closest('td').addClass('loading');

        $.ajax({
            type: 'POST',
            data: { guid: jRow.data('id') },
            context: this,
            url: url,
            success: function () {
                $(this).remove();
                jRow.addClass('protected');
            },
            error: function () {
                alert('Error occurred when trying to protect');
            },
            complete: function () {
                jCell.removeClass('loading');
            }
        });
        return false;
    });
    // allow range selection
    table.delegate('td', 'click', function (e) {
        if ($(this).closest('a').length) {
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
        var action, selected = $('.error.selected').not('.protected');
        
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
        selected.children('.actions').addClass('loading');

        $.ajax({
            type: 'POST',
            context: this,
            traditional: true,
            data: { ids: ids },
            dataType: 'json',
            url: baseUrl + action + '-list',
            success: function (data) {
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
                        selected.addClass('protected').find('.protect-link').remove();
                        break;
                }
            },
            error: function (a, b, c) {
                console.log(a, b, c);
                selected.children('.actions').removeClass('loading');
                alert('Error occurred when trying to ' + action + ' these errors.');
            }
        });
    });

    $('a.clear-all-link').click(function () {
        if (confirm('Really delete all non-protected errors?')) {
            $.ajax({
                type: 'POST',
                context: this,
                url: $(this).data('url'),
                success: function () {
                    window.location.reload(true);
                },
                error: function () {
                    alert('Error occurred when trying to delete all');
                },
                complete: function () {
                    //TODO: Loader/remove for this link
                }
            });
        }
        return false;
    });
});