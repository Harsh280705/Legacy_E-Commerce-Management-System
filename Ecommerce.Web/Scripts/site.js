/* Yantra Bazaar interactions — jQuery 3.4.1, no build step. */
(function ($) {
    'use strict';
    function token() {
        var t = $('input[name="__RequestVerificationToken"]').first().val();
        return t || '';
    }
    // All page tokens share one server-issued value; stamp it onto any form
    // (including AJAX-loaded cards whose own field renders empty) before submit.
    function stampToken($f) {
        var t = token();
        if (t) $f.find('input[name="__RequestVerificationToken"]').val(t);
        return t;
    }
    function notify(msg, kind, viewCart) {
        var zone = $('#yb-toast-zone');
        if (!zone.length) {
            zone = $('<div id="yb-toast-zone" aria-live="polite" role="status"></div>');
            $('body').append(zone);
        }
        var el = $('<div class="yb-toast yb-toast-' + (kind || 'ok') + '"></div>').text(msg);
        if (viewCart) el.append(' <a href="/Cart/Index">View cart</a>');
        var close = $('<button type="button" class="yb-toast-x" aria-label="Dismiss">&times;</button>');
        close.on('click', function () { el.remove(); });
        el.prepend(close);
        zone.append(el);
        setTimeout(function () { el.fadeOut(300, function () { el.remove(); }); }, 6000);
    }
    function showError(msg) {
        notify(msg, 'err', false);
        var bar = $('.yb-alert.alert-danger').first();
        if (bar.length) { bar.text(msg).show(); }
    }
    function refreshCount() {
        return $.get('/Cart/Count').done(function (d) { $('#cart-count').text(d.count); }).fail(function () {});
    }
    function refreshMini(openAfter) {
        return $.get('/Cart/Mini').done(function (html) {
            $('#mini-cart-body').html(html);
            if (openAfter) openMini();
        }).fail(function () {
            $('#mini-cart-body').html('<p class="yb-empty">Could not load cart. <a href="/Cart/Index">View cart</a></p>');
        });
    }
    function openMini() {
        var p = $('#mini-cart-panel'), b = $('#mini-cart-btn');
        p.show(); b.attr('aria-expanded', 'true');
    }
    function closeMini() {
        var p = $('#mini-cart-panel'), b = $('#mini-cart-btn');
        p.hide(); b.attr('aria-expanded', 'false');
    }
    function miniOpen() { return $('#mini-cart-panel').is(':visible'); }
    function setBusy($btn, busy, label) {
        if (!$btn.length) return;
        if (busy) { $btn.data('yb-label', $btn.html()); $btn.prop('disabled', true).addClass('yb-busy'); if (label) $btn.text(label); }
        else { $btn.prop('disabled', false).removeClass('yb-busy'); if ($btn.data('yb-label') !== undefined) $btn.html($btn.data('yb-label')); }
    }
    function cartMessage($root) {
        var m = ($root.attr('data-message') || '').replace(/^Added to cart\.?$/, '');
        var ok = $root.attr('data-ok') !== 'false';
        if (m) { if (ok) notify(m, 'ok', false); else showError(m); }
    }
    // ---- infinite scroll state ----
    var inf = { busy: false, seen: {} };
    function gridParams() { return window.YB_GRID || {}; }
    function markSeen($grid) {
        $grid.find('[data-pid]').each(function () { inf.seen[$(this).attr('data-pid')] = 1; });
    }
    function gridDataUrl(page) {
        var g = gridParams();
        var url = '/Catalog/GridData?page=' + page + '&sort=' + encodeURIComponent(g.sort || 'recommended') + '&size=' + encodeURIComponent(g.size || 12);
        if (g.slug) url += '&slug=' + encodeURIComponent(g.slug);
        else url += '&q=' + encodeURIComponent(g.q || '');
        if (g.cat) url += '&categoryId=' + encodeURIComponent(g.cat);
        return url;
    }
    function loadMore(retryBtn) {
        var g = gridParams(), zone = $('#infinite-zone');
        if (!zone.length || g.mode !== 'infinite' || inf.busy) return;
        if (zone.attr('data-hasmore') !== 'true') return;
        inf.busy = true;
        var page = parseInt(zone.attr('data-next'), 10) || 1;
        $('#infinite-status').text('Loading more products...');
        if (retryBtn) retryBtn.prop('disabled', true).text('Loading...');
        $.getJSON(gridDataUrl(page))
            .done(function (d) {
                var added = 0;
                if (d && d.html) {
                    var tmp = $('<div></div>').html(d.html);
                    tmp.find('[data-pid]').each(function () {
                        var pid = $(this).attr('data-pid');
                        if (inf.seen[pid]) $(this).remove(); else { inf.seen[pid] = 1; added++; }
                    });
                    $('#product-grid .yb-grid').append(tmp.find('.yb-card'));
                    if (!$('#product-grid .yb-grid').length) $('#product-grid').append(tmp.children());
                }
                var more = !!(d && d.hasMore);
                zone.attr('data-hasmore', more ? 'true' : 'false');
                zone.attr('data-next', d && d.nextPage ? d.nextPage : page + 1);
                $('#infinite-status').text(more ? '' : (added === 0 && page > 1 ? 'No more products.' : ''));
                if (!more) $('#load-more').prop('disabled', true).text('No more products');
                else if (retryBtn) retryBtn.prop('disabled', false).text('Load more');
            })
            .fail(function (xhr) {
                $('#infinite-status').text('Could not load more (HTTP ' + xhr.status + ').');
                var rb = $('#load-more');
                rb.prop('disabled', false).text('Retry');
                rb.off('click.ybretry').on('click.ybretry', function () { loadMore(rb); });
                showError('Infinite scroll failed (HTTP ' + xhr.status + '). Your filters are unchanged.');
            })
            .always(function () { inf.busy = false; });
    }
    $(function () {
        refreshCount();
        refreshMini(false);
        markSeen($('#product-grid'));
        // Mini-cart dropdown: predictable open/close, keyboard friendly.
        // The toggle is a real link (works without JS); with JS it opens the panel.
        $(document).on('click', '#mini-cart-btn', function (e) {
            e.preventDefault(); e.stopPropagation();
            if (miniOpen()) closeMini(); else { refreshMini(false); openMini(); }
        });
        $(document).on('click', '#mini-cart-panel', function (e) { e.stopPropagation(); });
        $(document).on('click', function () { closeMini(); });
        $(document).on('keydown', function (e) { if (e.keyCode === 27) { closeMini(); $('#mini-cart-btn').focus(); } });
        // Add to cart (AJAX, falls back to normal POST).
        $(document).on('submit', '.yb-add-form, #buy-form', function (e) {
            var $f = $(this);
            e.preventDefault();
            var $btn = $f.find('[type="submit"]');
            if ($btn.data('yb-busy')) return;
            $btn.data('yb-busy', 1);
            setBusy($btn, true, 'Adding...');
            stampToken($f);
            var pname = $btn.attr('data-name') || 'Product';
            var qty = parseInt($f.find('input[name="quantity"]').val(), 10) || 1;
            $.ajax({
                url: $f.attr('action') || '/Cart/Add', method: 'POST',
                data: $f.serialize()
            }).done(function (d) {
                if (d && d.success) {
                    $('#cart-count').text(d.count);
                    refreshMini(true);
                    notify(pname + ' × ' + qty + ' added to cart.', 'ok', true);
                } else showError((d && d.message) || 'Could not add to cart.');
            }).fail(function (xhr) {
                if (xhr.status === 0) showError('Network problem — the product may not have been added. Check your cart.');
                else showError('Add to cart failed (HTTP ' + xhr.status + '). Nothing was added.');
            }).always(function () { $btn.data('yb-busy', 0); setBusy($btn, false); });
        });
        // Quantity +/- steppers.
        $(document).on('click', '.yb-qty-btn', function () {
            var id = $(this).data('id'), delta = parseInt($(this).data('delta'), 10) || 0;
            var $in = $('.yb-qty[data-id="' + id + '"]');
            var cur = parseInt($in.val(), 10) || 0;
            var next = cur + delta;
            if (next < 0) next = 0;
            if (next > 99) next = 99;
            $in.val(next).trigger('change');
        });
        // Quantity change (AJAX partial refresh of #cart-lines).
        var qtyTimer = null;
        $(document).on('change', '.yb-qty', function () {
            var $in = $(this);
            clearTimeout(qtyTimer);
            qtyTimer = setTimeout(function () {
                var id = $in.data('id'), qty = parseInt($in.val(), 10);
                if (isNaN(qty) || qty < 0) { $in.val($in.attr('value') || 1); showError('Enter a quantity from 0 to 99.'); return; }
                if (qty > 99) { qty = 99; $in.val(99); }
                var row = $in.closest('tr'); row.addClass('yb-row-busy');
                $.ajax({
                    url: '/Cart/Update', method: 'POST',
                    data: { cartItemId: id, quantity: qty, __RequestVerificationToken: token() }
                }).done(function (html) {
                    var $new = $(html);
                    $('#cart-lines').replaceWith($new);
                    cartMessage($new);
                    refreshCount(); refreshMini(false);
                }).fail(function (xhr) { showError('Quantity update failed (HTTP ' + xhr.status + '). Cart unchanged.'); })
                  .always(function () { row.removeClass('yb-row-busy'); });
            }, 350);
        });
        $(document).on('click', '.yb-remove', function (e) {
            e.preventDefault();
            var $b = $(this), id = $b.data('id'), nm = $b.data('name') || 'Item';
            if ($b.data('yb-busy')) return;
            $b.data('yb-busy', 1); setBusy($b, true, 'Removing...');
            $.ajax({
                url: '/Cart/Remove', method: 'POST',
                data: { cartItemId: id, __RequestVerificationToken: token() }
            }).done(function (html) {
                var $new = $(html);
                $('#cart-lines').replaceWith($new);
                cartMessage($new);
                notify(nm + ' removed.', 'ok', false);
                refreshCount(); refreshMini(false);
            }).fail(function (xhr) { showError('Remove failed (HTTP ' + xhr.status + '). Item is still in your cart.'); })
              .always(function () { $b.data('yb-busy', 0); });
        });
        // Gallery.
        $(document).on('click', '.yb-thumb-sel', function () {
            $('#main-img').attr('src', $(this).attr('src'));
            $('.yb-thumb-sel').removeClass('yb-thumb-on');
            $(this).addClass('yb-thumb-on');
        });
        // Variant selection: live price + stock from option data (server re-validates on add).
        function syncVariant() {
            var $sel = $('#variantId');
            if (!$sel.length) return;
            var $opt = $sel.find('option:selected');
            var base = parseFloat($sel.attr('data-base-price')) || 0;
            var adj = parseFloat($opt.attr('data-adj')) || 0;
            var stock = parseInt($opt.attr('data-stock'), 10);
            $('#buy-price').text('₹' + (base + adj).toFixed(2));
            if (!isNaN(stock)) {
                $('#buy-stock').html(stock > 0
                    ? '<span class="label label-success">In stock (' + stock + ')</span>'
                    : '<span class="label label-danger">Out of stock</span>');
                $('#buy-form [type="submit"]').prop('disabled', stock <= 0);
            }
        }
        $(document).on('change', '#variantId', syncVariant);
        syncVariant();
        // AJAX filter/sort/pager for grids that define YB_GRID (pagination mode).
        $(document).on('change', '#sort-sel, #size-sel', function () {
            var g = gridParams();
            if (g.mode === 'infinite') return; // full reload keeps mode + filters
            var sort = $('#sort-sel').val() || g.sort || 'recommended';
            var url = g.slug ? '/Catalog/Grid?slug=' + encodeURIComponent(g.slug) + '&sort=' + encodeURIComponent(sort)
                             : '/Catalog/Grid?q=' + encodeURIComponent(g.q || '') + '&sort=' + encodeURIComponent(sort);
            $('#product-grid').addClass('yb-loading');
            $.get(url).done(function (html) { $('#product-grid').html(html); markSeen($('#product-grid')); })
                      .fail(function (xhr) { showError('Filter failed (HTTP ' + xhr.status + ').'); })
                      .always(function () { $('#product-grid').removeClass('yb-loading'); });
        });
        $(document).on('click', '#pager a', function (e) {
            var g = gridParams();
            if (g.mode === 'infinite' || (!g.slug && g.q === undefined && g.cat === undefined)) return; // allow normal nav
            e.preventDefault();
            var page = $(this).data('page') || 1;
            var sort = $('#sort-sel').val() || g.sort || 'name_asc';
            var url = g.slug ? '/Catalog/Grid?slug=' + encodeURIComponent(g.slug) + '&sort=' + encodeURIComponent(sort) + '&page=' + page
                             : '/Catalog/Grid?q=' + encodeURIComponent(g.q || '') + '&sort=' + encodeURIComponent(sort) + '&page=' + page;
            $('#product-grid').addClass('yb-loading');
            $.get(url).done(function (html) { $('#product-grid').html(html); markSeen($('#product-grid')); $('html,body').scrollTop($('#product-grid').offset().top - 80); })
                      .fail(function (xhr) { showError('Page load failed (HTTP ' + xhr.status + ').'); })
                      .always(function () { $('#product-grid').removeClass('yb-loading'); });
        });
        // Infinite scroll: explicit button + gentle auto-load near bottom (single-flight, deduped).
        $(document).on('click', '#load-more', function () { loadMore($(this)); });
        var scrollT = null;
        $(window).on('scroll', function () {
            var g = gridParams();
            if (g.mode !== 'infinite' || !$('#infinite-zone').length || inf.busy) return;
            clearTimeout(scrollT);
            scrollT = setTimeout(function () {
                var z = $('#infinite-zone');
                if (z.attr('data-hasmore') !== 'true') return;
                var top = z.offset().top;
                if ($(window).scrollTop() + $(window).height() > top - 600) loadMore(null);
            }, 200);
        });
        // Checkout: prevent double submit (also guarded server-side by idempotency key).
        $(document).on('submit', '#checkout-form, #confirm-form', function () {
            var $b = $(this).find('[type="submit"]');
            if ($b.data('busy')) return false;
            if (!$(this).valid || $(this).valid()) { $b.data('busy', 1).prop('disabled', true).text('Placing order...'); return true; }
            return false;
        });
        // Category lazy children (fills empty dropdown shells on first hover).
        $(document).on('mouseenter', '.yb-cat[data-children="1"]', function () {
            var $c = $(this);
            if ($c.data('yb-loaded')) return;
            $c.data('yb-loaded', 1);
            $.get('/Catalog/Children?id=' + $c.data('catid')).done(function (html) {
                $c.find('.yb-drop').html(html);
            }).fail(function () { $c.data('yb-loaded', 0); });
        });
    });
})(jQuery);
