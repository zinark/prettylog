var editor = null;
var query = DefaultQuery();

function DefaultQuery()
{
    return {
        query: '{}',
        start: moment().subtract('days', 1),
        end: moment(),
        limit: 1000,
        types: [],
        messages: []
    };
}

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
    logDensityControl: null,

    logsData: null,
    logsTable: null
};

google.load('visualization', '1', { 'packages': ['corechart', 'table', 'controls'] });
google.setOnLoadCallback(packagesLoaded);

var dataLoadEvents = {
    logsParsedSuccessfully: function (json)
    {
        logsData = new google.visualization.DataTable(json);
        logsTable = new google.visualization.Table(document.getElementById('divLogs'));
        logsTable.draw(logsData, { page: 'enable', pageSize: 50, allowHtml: true });
        $('#hits').text('Hits : ' + json.hits);
        google.visualization.events.addListener(logsTable, 'select', function ()
        {
            var selectedRow = logsTable.getSelection()[0].row;
            var selectedLine = {
                TimeStamp: logsData.getValue(selectedRow, 0),
                Type: logsData.getValue(selectedRow, 1),
                Message: logsData.getValue(selectedRow, 2),
                Object: logsData.getValue(selectedRow, 3),
            };
            queryFilters.logSelected(selectedLine);
        });

    },
    logsError: function (data)
    {
        console.log(data);
    },
    typesParsedSuccessfully: function (json)
    {
        variables.typesData = new google.visualization.DataTable(json);
        variables.typesTable = new google.visualization.Table(document.getElementById('divTypeDensity'));
        variables.typesChart = new google.visualization.PieChart(document.getElementById('divTypeDensityChart'));

        google.visualization.events.addListener(variables.typesChart, 'select', function ()
        {
            var selectedItem = variables.typesChart.getSelection()[0];
            if (selectedItem != null)
            {
                var value = variables.typesData.getValue(selectedItem.row, 0);
                queryFilters.typeFilterSelected(value);
            }
        });

        variables.typesChart.draw(variables.typesData,
            {
                'title': 'Type Density',
                'height': 200
            });

        variables.typesTable.draw(variables.typesData, { page: 'enable', pageSize: 5 });
    },
    typesError: function (data)
    {
        console.log(data);
    },
    messagesParsedSuccessfully: function (json)
    {
        variables.messagesData = new google.visualization.DataTable(json);
        variables.messagesTable = new google.visualization.Table(document.getElementById('divMessageDensity'));
        variables.messagesChart = new google.visualization.PieChart(document.getElementById('divMessageDensityChart'));

        google.visualization.events.addListener(variables.messagesChart, 'select', function ()
        {
            var selectedItem = variables.messagesChart.getSelection()[0];
            if (selectedItem != null)
            {
                var value = variables.messagesData.getValue(selectedItem.row, 0);
                queryFilters.messageFilterSelected(value);
            }
        });

        variables.messagesChart.draw(variables.messagesData,
            {
                'title': 'Message Density',
                'height': 200
            });

        variables.messagesTable.draw(variables.messagesData, { page: 'enable', pageSize: 5 });
    },
    messagesError: function (data)
    {
        console.log(data);
    },
    logDensityParsedSuccessfuly: function (json)
    {
        variables.logDensityData = new google.visualization.DataTable(json);

        // fix dates
        json.rows.forEach(function (row)
        {
            var val = row.c[0].v;
            var d = moment(val).toDate();
            row.c[0].v = d;
        });

        variables.logDensityTable = new google.visualization.Table(document.getElementById('divDashTable'));
        // logDensityTable.draw(logDensityData, { page: 'enable', pageSize: 5 });

        var dashboard = new google.visualization.Dashboard(document.getElementById('divDash'));

        variables.logDensityChart = new google.visualization.ChartWrapper({
            'chartType': 'ColumnChart',
            'containerId': 'divTimelineChart',
            'options': { 'legend': 'none' }
        });

        variables.logDensityControl = new google.visualization.ControlWrapper({
            'controlType': 'ChartRangeFilter',
            'containerId': 'divTimelineControl',
            'options': {
                'filterColumnIndex': 0,
                'state': { 'range': { 'start': moment().subtract('days', 3).toDate(), 'end': moment().toDate() } },
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

        google.visualization.events.addListener(variables.logDensityControl, 'statechange', function ()
        {
            var state = variables.logDensityControl.getState().range;
            queryFilters.dateFilterSelected(state.start, state.end);
        });

        google.visualization.events.addListener(variables.logDensityChart, 'select', function ()
        {
            var selectedItem = variables.logDensityChart.getChart().getSelection()[0];
            var value = variables.logDensityData.getValue(selectedItem.row, 0);
            queryFilters.daySelected(value);
        });


        dashboard.bind(variables.logDensityControl, variables.logDensityChart);
        dashboard.draw(variables.logDensityData);
    },
    logDensityError: function (data)
    {
        console.log(data);
    },
};

var queryFilters = {
    typeFilterSelected: function (type)
    {
        query.types = [type];
        ui.drawFilters();
    },
    messageFilterSelected: function (message)
    {
        query.messages = [message];
        ui.drawFilters();
    },
    dateFilterSelected: function (startDate, endDate)
    {
        query.start = startDate;
        query.end = endDate;
        ui.drawFilters();
    },
    daySelected: function (date)
    {
        query.start = date;
        query.end = moment(date).add('days', 1);
        ui.drawFilters();
    },
    queryRequested: function ()
    {
        query.query = editor.getSession().getValue();
        ui.drawFilters();
        ui.refreshViews();
    },
    newQueryPressed: function ()
    {
        query = DefaultQuery();
        ui.drawFilters();
        ui.refreshViews();
    },
    logSelected: function (log)
    {
        $('#txtTimeStamp').text(moment(log.TimeStamp).utc().format('DD/MM/YYYY hh:mm:ss'));
        $('#txtType').text(log.Type);
        $('#txtMessage').text(log.Message);
    }
};

var ui = {
    drawFilters: function ()
    {
        $('#queryFilters').html(JSON.stringify(query));
    },
    refreshViews: function ()
    {
        $('#loading').show();

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
            end: query.end,
            types: query.types,
            messages: query.messages
        };


        var a1 = $.ajax({
            type: 'post',
            dataType: 'json',
            url: '/Dashboard/Types',
            data: JSON.stringify(typesQuery),
            contentType: 'application/json; charset=utf-8',
            async: true,
            success: dataLoadEvents.typesParsedSuccessfully,
            error: dataLoadEvents.typesError
        });

        var a2 = $.ajax({
            type: 'post',
            dataType: 'json',
            url: '/Dashboard/Messages',
            data: JSON.stringify(messagesQuery),
            contentType: 'application/json; charset=utf-8',
            async: true,
            success: dataLoadEvents.messagesParsedSuccessfully,
            error: dataLoadEvents.messagesError
        });

        var a3 = $.ajax({
            type: 'post',
            dataType: 'json',
            url: '/Dashboard/LogDensities',
            data: JSON.stringify(logDensityQuery),
            contentType: 'application/json; charset=utf-8',
            async: true,
            success: dataLoadEvents.logDensityParsedSuccessfuly,
            error: dataLoadEvents.logDensityError
        });

        var a4 = $.ajax({
            type: 'post',
            dataType: 'json',
            url: '/Dashboard/Logs',
            data: JSON.stringify(query),
            contentType: 'application/json; charset=utf-8',
            async: true,
            success: dataLoadEvents.logsParsedSuccessfully,
            error: dataLoadEvents.logsError
        });




        $
            .when(a1, a2, a3, a4)

            .then(function ()
            {
                $('#loading').hide();
            }, function ()
            {
                $('#loading').hide();
            });


    }
};

function packagesLoaded()
{
    ui.refreshViews();
    ui.drawFilters();
}

function convertListToDict(list)
{

    var dict = [];
    var i = 0;
    list.forEach(function (val)
    {
        dict[i] = val;
        i++;
    });

    return dict;
}

$(document).ready(function ()
{

    $('#btnQuery').click(queryFilters.queryRequested);

    $('#editor').keydown(function (e)
    {
        if (e.ctrlKey && e.keyCode == 13)
        {
            queryFilters.queryRequested();
        }
    });

    $('#btnNewQuery').click(queryFilters.newQueryPressed);
    editor = ace.edit("editor");
    editor.setTheme("ace/theme/twilight");
    editor.getSession().setMode("ace/mode/javascript");
    editor.setFontSize(14);
    editor.setOptions({
        maxLines: Infinity,
        enableBasicAutocompletion: true,
        enableSnippets: true
    });

});
