var editor = null;
var query = DefaultQuery();

var prettyColors = ['#171717', '#242424', '#303030', '#3d3d3d', '#4a4a4a', '#575757'];
var cssNames = {
    headerRow: 'headerRow',
    tableRow: 'tableRow',
    oddTableRow: 'oddTableRow',
    selectedTableRow: 'selectedTableRow',
    hoverTableRow: 'hoverTableRow',
    headerCell: 'headerCell',
    tableCell: 'tableCell',
    rowNumberCell: 'rowNumberCell',
};

function DefaultQuery()
{
    return {
        query: '{}',
        start: moment().subtract('days', 1),
        end: moment(),
        limit: 100,
        types: [],
        messages: []
    };
}

var variables = {

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
        logsTable.draw(logsData, { page: 'enable', pageSize: 50, allowHtml: true, cssClassNames: cssNames });
        $('#hits').html(json.hits + ' hit(s)');
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
            'options': { 'legend': 'none', colors : prettyColors, backgroundColor : 'transparent' }
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
                        'colors': ['#cdcdcd'],
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
        query.end = moment();
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

var DynamicFilters =
{
    vars : {},
    add: function(fieldName, fieldQuery, divTableName, divChartName, onSelect) {

        var onError = function (data)
        {
            console.log('error ' + fieldName, data);
        };
        var onSuccess = function (json)
        {
            DynamicFilters.vars[fieldName + 'data'] = new google.visualization.DataTable(json);
            DynamicFilters.vars[fieldName + 'table'] = new google.visualization.Table(document.getElementById(divTableName));
            DynamicFilters.vars[fieldName + 'chart'] = new google.visualization.PieChart(document.getElementById(divChartName));

            google.visualization.events.addListener(DynamicFilters.vars[fieldName + 'chart'], 'select', function ()
            {
                var selectedItem = DynamicFilters.vars[fieldName + 'chart'].getSelection()[0];
                if (selectedItem != null)
                {
                    var value = DynamicFilters.vars[fieldName + 'data'].getValue(selectedItem.row, 0);
                    onSelect(value);
                }
            });

            DynamicFilters.vars[fieldName + 'chart'].draw(DynamicFilters.vars[fieldName + 'data'],
                {
                    'title': fieldName + ' Density',
                    'height': 300,
                    is3D: true,
                    fontSize: "8px",
                    fontName: "Arial",
                    colors: prettyColors,
                    legend: 'left',
                    //pieSliceText: 'label',
                    backgroundColor: '#dfdfdf',
                    pieSliceBorderColor: 'white',
                    pieSliceTextStyle: {
                        color: 'white', fontName: 'Times', fontSize: '8px'
                    }
                });

            DynamicFilters.vars[fieldName + 'table'].draw(DynamicFilters.vars[fieldName + 'data'], {
                page: 'enable',
                pageSize: 10,
                cssClassNames: cssNames
            });
        };
        
        return $.ajax({
            type: 'post',
            dataType: 'json',
            url: '/Dashboard/FieldDensity',
            data: JSON.stringify(fieldQuery),
            contentType: 'application/json; charset=utf-8',
            async: true,
            success: onSuccess,
            error: onError
        });
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

        var f1 = DynamicFilters.add('Type', { fieldName: 'Type', query: query.query, start: query.start, end: query.end }, 'divTypeDensity', 'divTypeDensityChart', function (value) { queryFilters.typeFilterSelected(value); });
        var f2 = DynamicFilters.add('Message', { fieldName : 'Message', query: query.query, start: query.start, end: query.end }, 'divMessageDensity', 'divMessageDensityChart', function (value) { queryFilters.messageFilterSelected(value); });
        var f3 = DynamicFilters.add('Ip', { fieldName : 'Ip', query: query.query, start: query.start, end: query.end }, 'divIpsDensity', 'divIpsDensityChart', function(value) { });
        var f4 = DynamicFilters.add('Host', { fieldName : 'Host', query: query.query, start: query.start, end: query.end }, 'divHostsDensity', 'divHostsDensityChart', function(value) { });

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
            url: '/Dashboard/LogDensities',
            data: JSON.stringify(logDensityQuery),
            contentType: 'application/json; charset=utf-8',
            async: true,
            success: dataLoadEvents.logDensityParsedSuccessfuly,
            error: dataLoadEvents.logDensityError
        });

        var a2 = $.ajax({
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
            .when(a1, a2, f1, f2, f3, f4)

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

$(document).ready(function ()
{

    $('#btnQuery').click(queryFilters.queryRequested);

    $(document).keydown(function (e)
    {
        if (e.ctrlKey && e.keyCode == 13)
        {
            queryFilters.queryRequested();
        }
    });
    $('#range').change(function ()
    {
        var r = $('#range').val();
        query.start = moment().subtract('days', r);
        ui.drawFilters();
    });

    $('#btnNewQuery').click(queryFilters.newQueryPressed);
    editor = ace.edit("editor");
    editor.setTheme("ace/theme/cobalt");
    editor.getSession().setMode("ace/mode/javascript");
    editor.setFontSize(14);
    editor.setOptions({
        maxLines: Infinity,
        enableBasicAutocompletion: true,
        enableSnippets: true
    });

});
