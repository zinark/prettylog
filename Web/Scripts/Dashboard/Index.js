var editor = null;

var query = {
    query: '{}',
    start: new Date(1999, 1, 1),
    end: new Date(),
    limit: 1000,
    types: [],
    messages: []
};

var variables = {

    typesData: null,
    typesTable: null,
    typesChart: null,

    messagesData: null,
    messagesTable: null,
    messagesChart: null,

    logDensityData: null,
    logDensityTable: null,
    logDensityChart: null,
    logDensityControl : null,

    logsData: null,
    logsTable: null
};

google.load('visualization', '1', { 'packages': ['corechart', 'table', 'controls'] });
google.setOnLoadCallback(packagesLoaded);

var events = {
    logsParsedSuccessfully: function (json) {
        logsData = new google.visualization.DataTable(json);
        logsTable = new google.visualization.Table(document.getElementById('divLogs'));
        logsTable.draw(logsData, { page: 'enable', pageSize: 15 });
    },
    logsError: function (data) {
        console.log(data);
    },
    typesParsedSuccessfully: function (json) {
        typesData = new google.visualization.DataTable(json);
        typesTable = new google.visualization.Table(document.getElementById('divTypeDensity'));
        typesChart = new google.visualization.PieChart(document.getElementById('divTypeDensityChart'));

        google.visualization.events.addListener(typesChart, 'select', function () {
            var selectedItem = typesChart.getSelection()[0];
            var value = typesData.getValue(selectedItem.row, 0);
            queryFilters.typeFilterSelected(value);
        });

        typesChart.draw(typesData,
            {
                'title': 'Type Density',
                'height': 200
            });

        // typesTable.draw(typesData, { page: 'enable', pageSize: 5 });
    },
    typesError: function (data) {
        console.log(data);
    },
    messagesParsedSuccessfully: function (json) {
        messagesData = new google.visualization.DataTable(json);
        messagesTable = new google.visualization.Table(document.getElementById('divMessageDensity'));
        messagesChart = new google.visualization.PieChart(document.getElementById('divMessageDensityChart'));

        google.visualization.events.addListener(messagesChart, 'select', function () {
            var selectedItem = messagesChart.getSelection()[0];
            var value = messagesData.getValue(selectedItem.row, 0);
            queryFilters.messageFilterSelected(value);
        });

        messagesChart.draw(messagesData,
            {
                'title': 'Message Density',
                'height': 200
            });

        // messagesTable.draw(messagesData, { page: 'enable', pageSize: 5 });
    },
    messagesError: function (data) {
        console.log(data);
    },
    logDensityParsedSuccessfuly: function (json) {
        logDensityData = new google.visualization.DataTable(json);

        // fix dates
        json.rows.forEach(function (row) {
            var val = row.c[0].v;
            var d = moment(val).toDate();
            row.c[0].v = d;
        });

        logDensityTable = new google.visualization.Table(document.getElementById('divDashTable'));
        // logDensityTable.draw(logDensityData, { page: 'enable', pageSize: 5 });

        var dashboard = new google.visualization.Dashboard(document.getElementById('divDash'));

        logDensityChart = new google.visualization.ChartWrapper({
            'chartType': 'ColumnChart',
            'containerId': 'divTimelineChart',
            'options': { 'legend': 'none' }
        });

        logDensityControl = new google.visualization.ControlWrapper({
            'controlType': 'ChartRangeFilter',
            'containerId': 'divTimelineControl',
            'options': {
                'filterColumnIndex': 0,
                'state': { 'range': { 'start': moment().subtract('days', 7).toDate(), 'end': moment().toDate() } },
                ui: {
                    'labelStacking': 'horizontal',
                    'chartType': 'ScatterChart',
                    'chartView': { 'columns': [0, 1] },
                    'chartOptions': {
                        // 'chartArea': { left: 5, top: 0, width: "95%", height: "98%" },
                        // 'width': '100%',
                        'backgroundColor': 'transparent',
                        // 'height': 50,
                        'colors': ['#DDDDDD', '#EEEEEE'],
                        'hAxis': {
                            'baselineColor': 'white',
                            'format': 'dd/MM/yy',
                        }
                    },
                    'minRangeSize': 86400000
                }
            }
        });

        $("#btnRanges").click(function () {
            var r = logDensityControl.getState().range;
            queryFilters.dateFilterSelected(r.start, r.end);
        });

        dashboard.bind(logDensityControl, logDensityChart);
        dashboard.draw(logDensityData);
    },
    logDensityError: function (data) {
        console.log(data);
    },
};

var queryFilters = {
    typeFilterSelected: function (selected) {
        console.log('type filter selected ', selected);
    },
    messageFilterSelected: function (selected) {
        console.log('message filter selected ', selected);
    },
    dateFilterSelected: function (start, end) {
        console.log('date filter selected ', start, end);
    },
    queryRequested: function () {
        query.query = editor.getSession().getValue();
        ui.refreshViews();
    }
};

var ui = {
    refreshViews: function () {
        var typesQuery = {
            query: query.query,
            start: query.start,
            end: query.end
        };

        var messagesQuery = {
            query: query.query,
            start: query.start,
            end: query.end
        };

        var logDensityQuery = {
            query: query.query,
            start: query.start,
            end: query.end
        };

        $.ajax({
            type: 'post',
            dataType: 'json',
            url: '/Dashboard/Types',
            data: JSON.stringify(typesQuery),
            contentType: 'application/json; charset=utf-8',
            async: true,
            success: events.typesParsedSuccessfully,
            error: events.typesError
        });

        $.ajax({
            type: 'post',
            dataType: 'json',
            url: '/Dashboard/Messages',
            data: JSON.stringify(messagesQuery),
            contentType: 'application/json; charset=utf-8',
            async: true,
            success: events.messagesParsedSuccessfully,
            error: events.messagesError
        });

        $.ajax({
            type: 'post',
            dataType: 'json',
            url: '/Dashboard/LogDensities',
            data: JSON.stringify(logDensityQuery),
            contentType: 'application/json; charset=utf-8',
            async: true,
            success: events.logDensityParsedSuccessfuly,
            error: events.logDensityError
        });

        $.ajax({
            type: 'post',
            dataType: 'json',
            url: '/Dashboard/Logs',
            data: JSON.stringify(query),
            contentType: 'application/json; charset=utf-8',
            async: true,
            success: events.logsParsedSuccessfully,
            error: events.logsError
        });
    }
};

function packagesLoaded() {
    ui.refreshViews();
}

$(document).ready(function () {
    $('#btnQuery').click(queryFilters.queryRequested);

    editor = ace.edit("editor");
    editor.setTheme("ace/theme/textmate");
    editor.getSession().setMode("ace/mode/javascript");
    editor.setFontSize(14);


});
