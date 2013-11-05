var dataPie, dataColumn;
var piechart, columnchart;

google.load('visualization', '1', { 'packages': ['corechart', 'table', 'controls'] });
google.setOnLoadCallback(drawChart);

function drawChart()
{
    var dashboard = new google.visualization.Dashboard(document.getElementById('divDash'));

    var chart = new google.visualization.ChartWrapper({
        'chartType': 'AreaChart',
        'containerId': 'divChart',
        'options': {
            // 'width': 300,
            // 'height': 300,
            // 'pieSliceText': 'value',
            'legend': 'right'
        }
    });

    dashboard.bind(new google.visualization.ControlWrapper({
        'controlType': 'NumberRangeFilter',
        'containerId': 'divControlType',
        'options': {
            'filterColumnLabel': 'Kobi'
        }
    }), chart);

    dashboard.bind(new google.visualization.ControlWrapper({
        'controlType': 'ChartRangeFilter',
        'containerId': 'divControlTimestamp',
        'options': {
            'filterColumnLabel': 'Timestamp'
        }
    }), chart);
    
    dashboard.draw(data.columnJson);
    
    google.visualization.events.addListener(dashboard, 'select', function () { console.log('select!') });

    drawBasics();
}

function drawBasics() {
    dataColumn = new google.visualization.DataTable(data.columnJson);
    dataPie = new google.visualization.DataTable(data.pieJson);

    piechart = new google.visualization.PieChart(document.getElementById('divChartPie'));
    google.visualization.events.addListener(piechart, 'select', selectHandler);
    piechart.draw(dataPie, {
        'title': 'Log Types',
        'width': 400,
        'height': 300
    });

    columnchart = new google.visualization.ColumnChart(document.getElementById('divChartColumn'));
    // google.visualization.events.addListener(columnchart, 'select', selectHandler);
    columnchart.draw(dataColumn, {
        title: 'Distrubution Of Logs By Type',
        hAxis: { title: 'TimeStamp', titleTextStyle: { color: 'red' } }
    });

    tablePie = new google.visualization.Table(document.getElementById('divTablePie'));
    google.visualization.events.addListener(tablePie, 'select', tableselectHandler);
    tablePie.draw(dataPie, {});

    tableColumn = new google.visualization.Table(document.getElementById('divTableColumn'));
    google.visualization.events.addListener(tableColumn, 'select', tableselectHandler);
    tableColumn.draw(dataColumn, {});
}

function selectHandler()
{
    var selectedItem = piechart.getSelection()[0];
    var value = dataPie.getValue(selectedItem.row, 0);
    alert('The user selected ' + value);
}

function tableselectHandler()
{
    var selectedItem = table.getSelection()[0];
    var value = dataPie.getValue(selectedItem.row, 0);
    alert('The user selected ' + value);
}

var data =
{
    pieJson: {
        cols: [{ id: 'Type', label: 'Type', type: 'string' },
            { id: 'Hits', label: 'Hits', type: 'number' },
            { id: 'BestHitOn', label: 'BestHitOn', type: 'date' }
        ],
        rows: [{ c: [{ v: 'Kobi' }, { v: 1000.0 }, { v: new Date(2008, 1, 28, 0, 31, 26), f: '2/28/08 12:31 AM' }] },
            { c: [{ v: 'Pro' }, { v: 670.0 }, { v: new Date(2008, 2, 30, 0, 31, 26), f: '3/30/08 12:31 AM' }] },
            { c: [{ v: 'Plus' }, { v: 330.0 }, { v: new Date(2008, 3, 30, 0, 31, 26), f: '4/30/08 12:31 AM' }] }
        ],
        p: { foo: 'hello', bar: 'world!' }
    },

    columnJson: {
        cols: [
            { id: 'Timestamp', label: 'Timestamp', type: 'date' },
            { id: 'Kobi', label: 'Kobi', type: 'number' },
            { id: 'Pro', label: 'Pro', type: 'number' },
            { id: 'Plus', label: 'Plus', type: 'number' }
        ],
        rows: [
            { c: [{ v: new Date(2008, 1, 28, 0, 31, 26), f: '2/28/08 12:31 AM' }, { v: 100 }, { v: 90 }, { v: 50 }] },
            { c: [{ v: new Date(2008, 1, 29, 0, 31, 26), f: '2/29/08 12:31 AM' }, { v: 120 }, { v: 70 }, { v: 20 }] },
            { c: [{ v: new Date(2008, 1, 30, 0, 31, 26), f: '2/30/08 12:31 AM' }, { v: 110 }, { v: 10 }, { v: 30 }] }
        ],
        p: { foo: 'hello', bar: 'world!' }
    }
};

function generateColumnData() {
    var result = [];
    for (var i = 0; i < 1000; i++) {
        result.push(
            { c: [{ v: new Date(2013 - i, 1, 28, 0, 31, 26) }, { v: 100 - i * 0.01 }, { v: 90 + i * 0.01 }, { v: 50 - i * 0.05 }] });
    }

    return result;
}
