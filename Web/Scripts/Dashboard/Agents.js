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
        start: moment().subtract('days', 7),
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
    }
};

var queryFilters = {
    queryRequested: function() {
        query.query = '{}';
        query.end = moment();
        query.skip = 0;
        ui.refreshViews();
    }
};


var ui = {
    refreshViews: function ()
    {
        var logDensityQuery = {
            ip : "",
            query: query.query,
            start: query.start,
            end: query.end,
            types: query.types,
            messages: query.messages
        };

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
    }
};

function packagesLoaded()
{
    ui.refreshViews();
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
    });
});