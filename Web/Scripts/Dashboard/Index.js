google.load('visualization', '1', { 'packages': ['corechart', 'table', 'controls'] });
google.setOnLoadCallback(packagesLoaded);

var events = {
    logsParsedSuccessfully: function (json)
    {
        data = new google.visualization.DataTable(json);
        table = new google.visualization.Table(document.getElementById('divLogs'));
        table.draw(data, { page: 'enable', pageSize: 15 });
    },
    logsError: function (data)
    {
        console.log(data);
    },
    typesParsedSuccessfully: function (json)
    {
        data = new google.visualization.DataTable(json);
        table = new google.visualization.Table(document.getElementById('divTypeDensity'));

        piechart = new google.visualization.PieChart(document.getElementById('divTypeDensityChart'));
        google.visualization.events.addListener(piechart, 'select', function ()
        {
            queryFilters.typeFilterSelected('job.a');
        });

        piechart.draw(data,
            {
                'title': 'Type Density',
                'height': 200
            });

        // table.draw(data, { page: 'enable', pageSize: 5 });
    },
    typesError: function (data)
    {
        console.log(data);
    },
    messagesParsedSuccessfully: function (json)
    {
        data = new google.visualization.DataTable(json);
        table = new google.visualization.Table(document.getElementById('divMessageDensity'));

        piechart = new google.visualization.PieChart(document.getElementById('divMessageDensityChart'));
        google.visualization.events.addListener(piechart, 'select', function ()
        {
            queryFilters.messageFilterSelected('not found');
        });

        piechart.draw(data,
            {
                'title': 'Message Density',
                'height': 200
            });

        // table.draw(data, { page: 'enable', pageSize: 5 });
    },
    messagesError: function (data)
    {
        console.log(data);
    },
    logDensityParsedSuccessfuly : function(json) {
        data = new google.visualization.DataTable(json);
        // fix dates
        json.rows.forEach(function(row) {
            var val = row.c[0].v;
            var d = moment(val).toDate();
            row.c[0].v = d;
        });

        table = new google.visualization.Table(document.getElementById('divDashTable'));
        // table.draw(data, { page: 'enable', pageSize: 5 });

        var dashboard = new google.visualization.Dashboard(document.getElementById('divDash'));

        var chart = new google.visualization.ChartWrapper({
            'chartType': 'ColumnChart',
            'containerId': 'divTimelineChart',
            'options': { 'legend': 'none' }
        });

        var control = new google.visualization.ControlWrapper({
            'controlType': 'ChartRangeFilter',
            'containerId': 'divTimelineControl',
            'options': {
                'filterColumnIndex': 0,
                'state': { 'range': { 'start': moment().subtract('days', 7).toDate(), 'end': moment().toDate() } },
                ui: {
                    'labelStacking': 'horizontal',
                    'chartType': 'ScatterChart',
                    'chartView': { 'columns': [0,1] },
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
        
        $("#btnRanges").click(function ()
        {
            var r = control.getState().range;
            queryFilters.dateFilterSelected(r.start, r.end);
        });

        
        dashboard.bind(control, chart);
        dashboard.draw(data);
    },
    logDensityError: function (data)
    {
        console.log(data);
    },
};

var queryFilters = {
    typeFilterSelected: function (selected)
    {
        console.log('type filter selected ', selected);
    },
    messageFilterSelected: function (selected)
    {
        console.log('message filter selected ', selected);
    },
    dateFilterSelected : function(start, end) {
        console.log('date filter selected ', start,end);
    }
};

function packagesLoaded()
{
    var logsQuery = {
        query: '{}',
        types: [],
        start: new Date(1999, 0, 0, 0, 0, 0),
        end: new Date(),
        limit: 1000
    };

    var typesQuery = {
        query: '{}',
        start: new Date(1999, 0, 0, 0, 0, 0),
        end: new Date()
    };

    var messagesQuery = {
        query: '{}',
        start: new Date(1999, 0, 0, 0, 0, 0),
        end: new Date()
    };

    var logDensityQuery = {
        query: '{}',
        start: new Date(1999, 0, 0, 0, 0, 0),
        end: new Date()
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
        data: JSON.stringify(logsQuery),
        contentType: 'application/json; charset=utf-8',
        async: true,
        success: events.logsParsedSuccessfully,
        error: events.logsError
    });
}

;