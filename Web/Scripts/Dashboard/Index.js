var editor = null;
var query = DefaultQuery();

var prettyColors = [
    '#80DFFF',
    '#FF9F80',
    '#BFDDA1',
    '#A1DDA1',
    '#A1DDBF',
    '#A1DDDD',
    '#A1BFDD',
    '#A1A1DD',
    '#BFA1DD',
    '#DDA1DD',
    '#DDA1BF',
    '#DDA1A1',
    '#DDBFA1',
    '#CDCD74',
    '#BDBD47',
    '#7474CD'
];

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
        start: moment().subtract('hours', 1),
        end: moment(),
        limit: 45,
        skip: 0,
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
        logsTable.draw(logsData, { firstRowNumber: query.skip + 1, showRowNumber: true, allowHtml: true, cssClassNames: cssNames });
        $('#hits').html(json.hits + ' hit(s)');
        $('#totalHits').html(json.hits + ' hit(s)');
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
    machineStatusParsedSuccessfuly: function (json)
    {
        // fix dates
        json.rows.forEach(function (row)
        {
            var val = row.c[0].v;
            var d = moment(val).toDate();
            row.c[0].v = d;
        });

        var data = new google.visualization.DataTable(json);
        var table = new google.visualization.Table(document.getElementById('divMachineStatus'));
        table.draw(data, { allowHtml: true, cssClassNames: cssNames });

        var chart = new google.visualization.ScatterChart(document.getElementById('divMachineStatusChart'));

        var options = {
            colors: prettyColors,
            colorAxis: { colors: ['#80DFFF', '#FF9F80'] },
            backgroundColor: 'transparent',
            title: 'Machine Status'
        };
        chart.draw(data, options);

        var columns = [];
        var series = {};
        for (var i = 0; i < data.getNumberOfColumns() ; i++)
        {
            columns.push(i);
            if (i > 0)
            {
                series[i - 1] = {};
            }
        }
        google.visualization.events.addListener(chart, 'select', function ()
        {
            var sel = chart.getSelection();
            // if selection length is 0, we deselected an element
            if (sel.length > 0)
            {
                // if row is undefined, we clicked on the legend
                if (typeof sel[0].row === 'undefined')
                {
                    var col = sel[0].column;
                    if (columns[col] == col)
                    {
                        // hide the data series
                        columns[col] = {
                            label: data.getColumnLabel(col),
                            type: data.getColumnType(col),
                            calc: function ()
                            {
                                return null;
                            }
                        };

                        // grey out the legend entry
                        series[col - 1].color = '#CCCCCC';
                    }
                    else
                    {
                        // show the data series
                        columns[col] = col;
                        series[col - 1].color = null;
                    }
                    var view = new google.visualization.DataView(data);
                    view.setColumns(columns);
                    chart.draw(view, options);
                }
            }
        });
    },
    machineStatusError: function (data)
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
        variables.logDensityTable.draw(variables.logDensityData, { page: 'enable', pageSize: 10, cssClassNames: cssNames });

        var dashboard = new google.visualization.Dashboard(document.getElementById('divDash'));

        variables.logDensityChart = new google.visualization.ChartWrapper({
            'chartType': 'ColumnChart',
            'containerId': 'divTimelineChart',
            'options': { 'legend': 'none', colors: prettyColors, backgroundColor: 'transparent' }
        });

        variables.logDensityControl = new google.visualization.ControlWrapper({
            'controlType': 'ChartRangeFilter',
            'containerId': 'divTimelineControl',
            'options': {
                'filterColumnIndex': 0,
                'state': { 'range': { 'start': moment().subtract('days', 3).toDate(), 'end': moment().toDate() } },
                ui: {
                    //'labelStacking': 'horizontal',
                    'chartType': 'ScatterChart',
                    'chartView': { 'columns': [0, 1] },

                    'chartOptions': {
                        // 'chartArea': { left: 5, top: 0, width: "95%", height: "98%" },
                        // 'width': '100%',
                        'backgroundColor': 'transparent',
                        // 'height': 50,
                        'colors': ['black'],
                        'hAxis': {
                            'baselineColor': 'white',
                            'format': 'dd / MM / yy',
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
        query.skip = 0;
        ui.drawFilters();
        ui.refreshViews();
    },
    clearFiltersPressed: function ()
    {
        query.types = null;
        query.messages = null;
        ui.drawFilters();
        ui.refreshViews();
    },
    prevPressed: function ()
    {
        if (query.skip > 0)
        {
            query.skip -= query.limit;
        }
        if (query.skip < 0) query.skip = 0;

        ui.refreshLogs();
    },
    nextPressed: function ()
    {
        query.skip += query.limit;
        ui.refreshLogs();
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
    vars: {},
    add: function (fieldName, fieldQuery, divTableName, divChartName, onSelect)
    {
        DynamicFilters.vars[fieldName + 'charthtml'] = $('#' + divChartName).html();
        DynamicFilters.vars[fieldName + 'tablehtml'] = $('#' + divChartName).html();

        var onError = function (data)
        {
            console.log('error ' + fieldName, data);
        };
        var onSuccess = function (json)
        {
            $('#' + divChartName).html(DynamicFilters.vars[fieldName + 'charthtml']);
            $('#' + divTableName).html(DynamicFilters.vars[fieldName + 'tablehtml']);
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

            google.visualization.events.addListener(DynamicFilters.vars[fieldName + 'table'], 'select', function ()
            {
                var selectedItem = DynamicFilters.vars[fieldName + 'table'].getSelection()[0];
                if (selectedItem != null)
                {
                    var value = DynamicFilters.vars[fieldName + 'data'].getValue(selectedItem.row, 0);
                    onSelect(value);
                }
            });

            DynamicFilters.vars[fieldName + 'chart'].draw(DynamicFilters.vars[fieldName + 'data'],
                {
                    'title': fieldName + ' Density',
                    height: 400,
                    width: 720,
                    is3D: true,
                    fontSize: "8px",
                    fontName: "Arial",
                    colors: prettyColors,
                    legend: 'left',
                    //pieSliceText: 'label',
                    // backgroundColor: '#dfdfdf',
                    pieSliceBorderColor: '#dfdfdf',
                    pieSliceTextStyle: {
                        color: 'black',
                        fontName: 'Times',
                        fontSize: '8px'
                    }
                });

            DynamicFilters.vars[fieldName + 'table'].draw(DynamicFilters.vars[fieldName + 'data'], {
                page: 'enable',
                pageSize: 10,
                cssClassNames: cssNames
            });
        };

        var waitHtml = $('#waitHtml').html();
        $('#' + divChartName).html(waitHtml);
        $('#' + divTableName).html('');

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
    refreshLogs: function ()
    {
        return $.ajax({
            type: 'post',
            dataType: 'json',
            url: '/Dashboard/Logs',
            data: JSON.stringify(query),
            contentType: 'application/json; charset=utf-8',
            async: true,
            success: dataLoadEvents.logsParsedSuccessfully,
            error: dataLoadEvents.logsError
        });
    },
    refreshViews: function ()
    {
        $('#loading').show();

        var logDensityQuery = {
            query: query.query,
            start: query.start,
            end: query.end,
            types: query.types,
            messages: query.messages
        };

        $.ajax({
            type: 'post',
            dataType: 'json',
            url: '/Dashboard/Timeline',
            data: JSON.stringify(logDensityQuery),
            contentType: 'application/json; charset=utf-8',
            async: true,
            success: dataLoadEvents.logDensityParsedSuccessfuly,
            error: dataLoadEvents.logDensityError
        });

        $.ajax({
            type: 'post',
            dataType: 'json',
            url: '/Dashboard/MachineStatus',
            data: JSON.stringify(logDensityQuery),
            contentType: 'application/json; charset=utf-8',
            async: true,
            success: dataLoadEvents.machineStatusParsedSuccessfuly,
            error: dataLoadEvents.machineStatusError
        });

        var ajxLogs = ui.refreshLogs();

        $
            .when(ajxLogs)
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




    $('#typeTrigger').click(function ()
    {
        DynamicFilters.add('Type', { fieldName: 'Type', query: query.query, start: query.start, end: query.end }, 'divTypeDensity', 'divTypeDensityChart', function (value) { queryFilters.typeFilterSelected(value); });
    });

    $('#messageTrigger').click(function ()
    {
        DynamicFilters.add('Message', { fieldName: 'Message', query: query.query, start: query.start, end: query.end }, 'divMessageDensity', 'divMessageDensityChart', function (value) { queryFilters.messageFilterSelected(value); });
    });

    $('#ipTrigger').click(function ()
    {
        DynamicFilters.add('Ip', { fieldName: 'Ip', query: query.query, start: query.start, end: query.end }, 'divIpsDensity', 'divIpsDensityChart', function (value) { });
    });

    $('#hostTrigger').click(function ()
    {
        DynamicFilters.add('Host', { fieldName: 'Host', query: query.query, start: query.start, end: query.end }, 'divHostsDensity', 'divHostsDensityChart', function (value) { });
    });


    $(document).keydown(function (e)
    {
        if (e.ctrlKey && e.keyCode == 13)
        {
            queryFilters.queryRequested();
        }
    });
    
    var start = null;
    $('#range').change(function ()
    {
        var r = $('#range').val();
        if (r == '1hour') start = moment().subtract('hours', 1);
        if (r == '6hour') start = moment().subtract('hours', 6);
        if (r == '12hour') start = moment().subtract('hours', 12);
        if (r == '1day') start = moment().subtract('day', 1);
        if (r == '7day') start = moment().subtract('day', 7);
        if (r == '14day') start = moment().subtract('day', 14);
        if (r == '30day') start = moment().subtract('day', 30);
        query.start = start;
        ui.drawFilters();
    });

    $('#btnClearFilters').click(queryFilters.clearFiltersPressed);
    $('#btnNext').click(queryFilters.nextPressed);
    $('#btnPrev').click(queryFilters.prevPressed);
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