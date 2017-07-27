$(function () {
    $("footer").append("<div>Example: This text was added by a custom JavaScript include: <b>errors.js</b></div>");

    // the Exception is exposed as a top level variable when on the detail page
    // all data present on the page is also available easily to JavaScript via this variable
    var ex = window.Exception;
    if (typeof (ex) === 'undefined') return;

    $('div.custom-data td.key:contains("User Id") + td').wrapInner(function() {
        return $('<a/>', { href: 'http://www.google.com/?q=' + encodeURIComponent(ex.CustomData["User Id"]), target: '_blank' });
    }).append(" < This wrapped in a link via the included <b>errors.js</b>");
});