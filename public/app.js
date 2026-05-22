$(document).ready(function () {
  // ===== Tab Navigation =====
  $('.tab-btn').on('click', function () {
    var targetTab = $(this).data('tab');
    $('.tab-btn').removeClass('active').attr('aria-selected', 'false');
    $(this).addClass('active').attr('aria-selected', 'true');
    $('.tab-content').removeClass('active').attr('hidden', true);
    $('#' + targetTab + '-panel').addClass('active').removeAttr('hidden');
  });

  // ===== Helper Functions =====
  function truncateText(text, maxLength) {
    if (!text) return '-';
    var str = typeof text === 'object' ? JSON.stringify(text) : String(text);
    if (str.length <= maxLength) return str;
    return str.substring(0, maxLength) + '...';
  }

  function formatTimestamp(isoString) {
    if (!isoString) return '-';
    var d = new Date(isoString);
    var year = d.getFullYear();
    var month = String(d.getMonth() + 1).padStart(2, '0');
    var day = String(d.getDate()).padStart(2, '0');
    var hours = String(d.getHours()).padStart(2, '0');
    var minutes = String(d.getMinutes()).padStart(2, '0');
    var seconds = String(d.getSeconds()).padStart(2, '0');
    return year + '-' + month + '-' + day + ' ' + hours + ':' + minutes + ':' + seconds;
  }

  function renderStatusBadge(status) {
    var badgeClass = '';
    switch (status) {
      case 'SUCCESS': badgeClass = 'badge-success'; break;
      case 'ERROR':   badgeClass = 'badge-error';   break;
      case 'PENDING': badgeClass = 'badge-pending';  break;
      default:        badgeClass = '';               break;
    }
    return '<span class="' + badgeClass + '">' + status + '</span>';
  }

  // ===== Load data from API =====
  var requestData = [];
  var errorData = [];

  function loadRequests(callback) {
    $.getJSON('/api/requests', function (data) {
      requestData = data;
      if (callback) callback(data);
    }).fail(function () {
      console.error('Failed to load requests from API');
      requestData = [];
      if (callback) callback([]);
    });
  }

  function loadErrors(callback) {
    $.getJSON('/api/errors', function (data) {
      errorData = data;
      if (callback) callback(data);
    }).fail(function () {
      console.error('Failed to load errors from API');
      errorData = [];
      if (callback) callback([]);
    });
  }

  // ===== Request Monitor DataTable =====
  var requestTable;

  loadRequests(function (data) {
    requestTable = $('#requestTable').DataTable({
      data: data,
      columns: [
        { data: 'appName', title: 'AppName', render: function (data) { return data ? data : '-'; } },
        { data: 'applicationName', title: 'Application' },
        { data: 'url', title: 'URL' },
        { data: 'httpMethod', title: 'HTTP Method' },
        {
          data: 'headers',
          title: 'Headers',
          render: function (data) { return truncateText(data, 50); }
        },
        {
          data: 'body',
          title: 'Body',
          render: function (data) { return truncateText(data, 50); }
        },
        { data: 'clientIpAddress', title: 'Client IP' },
        {
          data: 'requestTimestamp',
          title: 'Timestamp',
          render: function (data) { return formatTimestamp(data); }
        },
        {
          data: 'status',
          title: 'Status',
          render: function (data) { return renderStatusBadge(data); }
        },
        {
          data: 'responseTime',
          title: 'Response Time',
          render: function (data) {
            if (data === 0 || data === null || data === undefined) return '-';
            return data + ' ms';
          }
        },
        {
          data: 'status',
          title: 'Action',
          orderable: false,
          searchable: false,
          render: function (data, type, row) {
            if (data === 'ERROR') {
              return '<div class="action-buttons"><button class="btn-resend" data-request-id="' + row.id + '">Resend</button><button class="btn-ignore" data-request-id="' + row.id + '">Ignore</button></div>';
            }
            return '';
          }
        }
      ],
      order: [[7, 'desc']],
      pageLength: 20,
      lengthMenu: [10, 20, 50, 100],
      language: {
        info: 'แสดง _START_-_END_ จาก _TOTAL_ รายการ',
        infoEmpty: 'ไม่มีรายการ',
        infoFiltered: '(กรองจากทั้งหมด _MAX_ รายการ)',
        lengthMenu: 'แสดง _MENU_ รายการ',
        search: 'ค้นหา:',
        zeroRecords: 'ไม่พบข้อมูลที่ตรงกัน',
        paginate: { first: 'หน้าแรก', last: 'หน้าสุดท้าย', next: 'ถัดไป', previous: 'ก่อนหน้า' }
      }
    });

    registerRequestFilters();
  });

  // ===== Resend Functions =====
  var currentResendRequestId = null;

  function showModal(requestObj) {
    var details = '';
    details += '<p><strong>URL:</strong> ' + requestObj.url + '</p>';
    details += '<p><strong>HTTP Method:</strong> ' + requestObj.httpMethod + '</p>';
    details += '<p><strong>Headers:</strong> ' + (typeof requestObj.headers === 'object' ? JSON.stringify(requestObj.headers) : requestObj.headers) + '</p>';
    details += '<p><strong>Body:</strong> ' + (requestObj.body || '-') + '</p>';
    details += '<p><strong>Client IP:</strong> ' + requestObj.clientIpAddress + '</p>';
    details += '<p><strong>Timestamp:</strong> ' + formatTimestamp(requestObj.requestTimestamp) + '</p>';
    $('#modal-request-details').html(details);
    currentResendRequestId = requestObj.id;
    $('#resend-modal').removeAttr('hidden');
  }

  function hideModal() {
    $('#resend-modal').attr('hidden', true);
    $('#modal-request-details').html('');
    currentResendRequestId = null;
  }

  function showNotification(message, type) {
    var $notification = $('<div class="notification ' + type + '">' + message + '</div>');
    $('#notification-area').append($notification);
    setTimeout(function () {
      $notification.fadeOut(300, function () { $(this).remove(); });
    }, 3000);
  }

  // ===== Resend Button Handler =====
  $(document).on('click', '.btn-resend', function () {
    var reqId = $(this).data('request-id');
    var reqObj = requestData.find(function (r) { return r.id === reqId; });
    if (!reqObj) {
      refreshTables();
      return;
    }
    showModal(reqObj);
  });

  // ===== Modal Confirm =====
  $('#modal-confirm').on('click', function () {
    if (!currentResendRequestId) return;

    var $btn = $(this);
    $btn.prop('disabled', true).text('Sending...');

    $.ajax({
      url: '/api/requests/' + currentResendRequestId + '/resend',
      method: 'POST',
      contentType: 'application/json',
      success: function (result) {
        hideModal();
        $btn.prop('disabled', false).text('Resend');
        showNotification(result.message, result.success ? 'success' : 'error');
        refreshTables();
      },
      error: function () {
        hideModal();
        $btn.prop('disabled', false).text('Resend');
        showNotification('Resend ไม่สำเร็จ: เกิดข้อผิดพลาด', 'error');
      }
    });
  });

  $('#modal-cancel').on('click', function () { hideModal(); });
  $(document).on('click', '.modal-overlay', function () { hideModal(); });

  // ===== Ignore Button Handler =====
  $(document).on('click', '.btn-ignore', function () {
    var reqId = $(this).data('request-id');
    var $row = $(this).closest('tr');

    $.ajax({
      url: '/api/requests/' + reqId + '/ignore',
      method: 'PATCH',
      contentType: 'application/json',
      success: function (result) {
        $row.fadeOut(300, function () { $(this).remove(); });
        showNotification(result.message, 'success');
      },
      error: function (xhr) {
        showNotification('Ignore ไม่สำเร็จ: ' + (xhr.responseJSON?.error || 'Unknown error'), 'error');
      }
    });
  });

  // ===== Refresh tables =====
  function refreshTables() {
    loadRequests(function (data) {
      if (requestTable) {
        requestTable.clear().rows.add(data).draw();
      }
    });
    loadErrors(function (data) {
      if (errorTable) {
        errorTable.clear().rows.add(data).draw();
      }
    });
  }

  // ===== Request Filter Controls =====
  function registerRequestFilters() {
    $.fn.dataTable.ext.search.push(function (settings, data, dataIndex) {
      if (settings.nTable.id !== 'requestTable') return true;

      var methodFilter = $('#filter-http-method').val();
      var statusFilter = $('#filter-status').val();
      var urlFilter = $('#filter-url').val().toLowerCase();
      var dateFrom = $('#filter-date-from').val();
      var dateTo = $('#filter-date-to').val();

      var rowData = requestTable.row(dataIndex).data();
      if (!rowData) return true;

      if (methodFilter && rowData.httpMethod !== methodFilter) return false;
      if (statusFilter && rowData.status !== statusFilter) return false;
      if (urlFilter && rowData.url.toLowerCase().indexOf(urlFilter) === -1) return false;

      if (dateFrom || dateTo) {
        var rowDate = new Date(rowData.requestTimestamp);
        var rowDateStr = rowDate.getFullYear() + '-' +
          String(rowDate.getMonth() + 1).padStart(2, '0') + '-' +
          String(rowDate.getDate()).padStart(2, '0');
        if (dateFrom && rowDateStr < dateFrom) return false;
        if (dateTo && rowDateStr > dateTo) return false;
      }
      return true;
    });

    $('#filter-http-method').on('change', function () { requestTable.draw(); });
    $('#filter-status').on('change', function () { requestTable.draw(); });
    $('#filter-url').on('keyup', function () { requestTable.draw(); });
    $('#filter-date-from').on('change', function () { requestTable.draw(); });
    $('#filter-date-to').on('change', function () { requestTable.draw(); });

    $('#clear-request-filters').on('click', function () {
      $('#filter-http-method').val('');
      $('#filter-status').val('');
      $('#filter-url').val('');
      $('#filter-date-from').val('');
      $('#filter-date-to').val('');
      requestTable.search('').draw();
    });
  }

  // ===== Error Category Badge =====
  function renderErrorCategoryBadge(category) {
    var badgeClass = '';
    switch (category) {
      case 'CLIENT_ERROR':     badgeClass = 'badge-client-error';     break;
      case 'SERVER_ERROR':     badgeClass = 'badge-server-error';     break;
      case 'TIMEOUT_ERROR':    badgeClass = 'badge-timeout-error';    break;
      case 'CONNECTION_ERROR': badgeClass = 'badge-connection-error'; break;
      case 'GATEWAY_ERROR':    badgeClass = 'badge-gateway-error';    break;
      default:                 badgeClass = '';                        break;
    }
    return '<span class="' + badgeClass + '">' + category + '</span>';
  }

  // ===== Error Handler DataTable =====
  var errorTable;

  loadErrors(function (data) {
    errorTable = $('#errorTable').DataTable({
      data: data,
      columns: [
        { data: 'errorCode', title: 'Error Code' },
        { data: 'message', title: 'Message' },
        {
          data: 'stackTrace',
          title: 'Stack Trace',
          render: function (data) {
            if (!data) return '-';
            var truncated = data.length > 80 ? data.substring(0, 80) + '...' : data;
            return '<span class="stack-trace-cell" title="' + $('<span>').text(data).html() + '">' + $('<span>').text(truncated).html() + '</span>';
          }
        },
        {
          data: 'errorTimestamp',
          title: 'Error Timestamp',
          render: function (data) { return formatTimestamp(data); }
        },
        {
          data: 'errorCategory',
          title: 'Error Category',
          render: function (data) { return renderErrorCategoryBadge(data); }
        },
        {
          data: 'isResolved',
          title: 'Resolved',
          render: function (data) { return data ? 'Yes' : 'No'; }
        },
        {
          data: 'requestId',
          title: 'Action',
          orderable: false,
          searchable: false,
          render: function (data) {
            return '<div class="action-buttons"><button class="btn-resend" data-request-id="' + data + '">Resend</button><button class="btn-ignore" data-request-id="' + data + '">Ignore</button></div>';
          }
        }
      ],
      order: [[3, 'desc']],
      pageLength: 20,
      lengthMenu: [10, 20, 50, 100],
      language: {
        info: 'แสดง _START_-_END_ จาก _TOTAL_ รายการ',
        infoEmpty: 'ไม่มีรายการ',
        infoFiltered: '(กรองจากทั้งหมด _MAX_ รายการ)',
        lengthMenu: 'แสดง _MENU_ รายการ',
        search: 'ค้นหา:',
        zeroRecords: 'ไม่พบข้อมูลที่ตรงกัน',
        paginate: { first: 'หน้าแรก', last: 'หน้าสุดท้าย', next: 'ถัดไป', previous: 'ก่อนหน้า' }
      }
    });

    // Error filters
    $.fn.dataTable.ext.search.push(function (settings, data, dataIndex) {
      if (settings.nTable.id !== 'errorTable') return true;

      var categoryFilter = $('#filter-error-category').val();
      var errorCodeFilter = $('#filter-error-code').val().toLowerCase();
      var resolvedFilter = $('#filter-resolved').val();

      var rowData = errorTable.row(dataIndex).data();
      if (!rowData) return true;

      if (categoryFilter && rowData.errorCategory !== categoryFilter) return false;
      if (errorCodeFilter && rowData.errorCode.toLowerCase().indexOf(errorCodeFilter) === -1) return false;
      if (resolvedFilter !== '') {
        if (resolvedFilter === 'true' && !rowData.isResolved) return false;
        if (resolvedFilter === 'false' && rowData.isResolved) return false;
      }
      return true;
    });

    $('#filter-error-category').on('change', function () { errorTable.draw(); });
    $('#filter-error-code').on('keyup', function () { errorTable.draw(); });
    $('#filter-resolved').on('change', function () { errorTable.draw(); });

    $('#clear-error-filters').on('click', function () {
      $('#filter-error-category').val('');
      $('#filter-error-code').val('');
      $('#filter-resolved').val('');
      errorTable.search('').draw();
    });
  });
});
