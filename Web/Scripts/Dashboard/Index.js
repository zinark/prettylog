google.load('visualization', '1', { 'packages': ['corechart', 'table', 'controls'] });
google.setOnLoadCallback(drawTable);

var events = {
    successFind : function(json) {
        data = new google.visualization.DataTable(json);
        table = new google.visualization.Table(document.getElementById('divLogs'));
        table.draw(data, { page : 'enable', pageSize : 25});
    },
    errorFind : function(data) {
        console.log(data);
    },
    successTypes : function(json) {
        data = new google.visualization.DataTable(json);
        table = new google.visualization.Table(document.getElementById('divTypes'));
        table.draw(data, { page : 'enable', pageSize : 25});
    },
    errorTypes : function(data) {
        console.log(data);
    }
};

function drawTable () {

    var logsQuery = {
        query: '{}',
        types: [],
        start: new Date(1999, 0, 0, 0, 0, 0),
        end: new Date(),
        limit: 100
    };

    var typesQuery = {
        query: '{}',
        start: new Date(1999, 0, 0, 0, 0, 0),
        end: new Date()
    };

    $.ajax({
        type: 'post',
        dataType: 'json',
        url: '/Dashboard/Logs',
        data: JSON.stringify(logsQuery),
        contentType: 'application/json; charset=utf-8',
        async: true,
        success: events.successFind,
        error: events.errorFind
    });

    $.ajax({
        type: 'post',
        dataType: 'json',
        url: '/Dashboard/Types',
        data: JSON.stringify(logsQuery),
        contentType: 'application/json; charset=utf-8',
        async: true,
        success: events.successTypes,
        error: events.errorTypes
    });
};