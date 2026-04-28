import { ReactNode, useCallback, useEffect, useState } from 'react';
import Table from '../../components/Table';
import TableHead from '../../components/TableHead';
import TableRow from '../../components/TableRow';
import TableHeaderCell from '../../components/TableHeaderCell';
import TableBody from '../../components/TableBody';
import Pagination from '../../components/Pagination';
import TableCell from '../../components/TableCell';
import PageSubheader from '../../components/PageSubHeader';
import Button from '../../components/Button';
import { ExportDefintion, formatNumber, runSparqlQuery, SparqlQuery, SparqlQuerySorting, SparqlResult } from './tools';
import { IntColumn } from './types';
import { useEndpointUrl } from '../../client';

type Props<T> = {
    query: SparqlQuery;
    columns: Column<T>[];
    header?: string | undefined | null;
    exportDefintion?: ExportDefintion<T> | null | undefined;
    pageSize?: number;
};

type Column<T> = {
    header: string;
    value: (row: T) => ReactNode;
    sortProperty?: string | null | undefined;
};

function translateSorting(ascending: boolean | undefined | null): 'asc' | 'desc' | undefined {
    if (ascending === undefined || ascending === null) return undefined;
    return ascending ? 'asc' : 'desc';
}

export default function SparqlEnabledTable<T>(props: Props<T>) {
    const [data, setData] = useState<T[] | null>(null);
    const [page, setPage] = useState(1);
    const [sorting, setSorting] = useState<SparqlQuerySorting[]>([]);
    const [totalCount, setTotalCount] = useState<number | null>(null);

    const { query, exportDefintion, pageSize = 20 } = props;

    const endpointUrl = useEndpointUrl();

    useEffect(() => {
        async function fetchData() {
            setData(await runSparqlQuery(endpointUrl, query, pageSize, (page - 1) * pageSize, sorting));
        }

        fetchData();
    }, [query, page, sorting, pageSize, endpointUrl]);

    useEffect(() => {
        async function fetchTotalCount() {
            if (!endpointUrl) return;

            let q = (query.prefixes ?? []).map((p) => `PREFIX ${p}`).join('\n');
            q += `\nSELECT (COUNT(*) AS ?count)\n`;
            if (query.where) {
                q += `WHERE {\n${query.where.map((w) => `${w}`).join('\n')}\n}\n`;
            }
            const response = await fetch(endpointUrl + '?query=' + encodeURIComponent(q), {
                headers: { Accept: 'application/sparql-results+json' }
            });
            const result: SparqlResult<{ count: IntColumn }> = await response.json();
            setTotalCount(parseInt(result.results.bindings[0].count.value));
        }

        fetchTotalCount();
    }, [query, page, sorting, endpointUrl]);

    const doExport = useCallback(async () => {
        if (!exportDefintion) return;

        const data = await runSparqlQuery<T>(endpointUrl, query, null, null, []);
        let content = '';

        function escapeCsv(value: string | number): string {
            if (typeof value === 'number') {
                return value.toString();
            } else {
                value ??= '';
                value = value.replaceAll('"', '""');
                value = value.replaceAll(/(?:\r\n|\r|\n)/g, ' ');
                return `"${value}"`;
            }
        }

        content += exportDefintion.columns.map((c) => escapeCsv(c.header)).join(';') + '\n';
        for (const row of data) {
            content += exportDefintion.columns.map((c) => escapeCsv(c.value(row))).join(';') + '\n';
        }

        const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.setAttribute('href', url);
        link.setAttribute('download', 'export.csv');
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }, [exportDefintion, endpointUrl]);

    const header = props.header ? props.header.replace('%count%', totalCount !== null ? `(${formatNumber(totalCount)})` : '') : null;

    return (
        <div>
            {data !== null ? (
                <>
                    {header ? (
                        <PageSubheader>
                            {header}{' '}
                            {props.exportDefintion && totalCount !== null && totalCount > 0 ? (
                                <Button
                                    buttonType="secondary"
                                    style={{ marginBottom: 0, marginLeft: '20px' }}
                                    onClick={() => {
                                        doExport();
                                    }}
                                >
                                    Export
                                </Button>
                            ) : null}
                        </PageSubheader>
                    ) : null}
                    {data.length > 0 ? (
                        <Table>
                            <TableHead>
                                <TableRow>
                                    {props.columns.map((c, i) => (
                                        <TableHeaderCell
                                            key={i}
                                            enableSorting={c.sortProperty !== undefined && c.sortProperty !== null}
                                            sortingDirection={translateSorting(sorting.find((s) => s.propertyName === c.sortProperty)?.ascending)}
                                            toggleSortingDirection={() => {
                                                const currentDirection = sorting.find((s) => s.propertyName === c.sortProperty)?.ascending;
                                                const newDirection = currentDirection === undefined ? true : currentDirection ? false : undefined;
                                                console.log([currentDirection, newDirection]);
                                                setSorting(newDirection !== undefined ? [{ propertyName: c.sortProperty!, ascending: newDirection }] : []);
                                            }}
                                        >
                                            {c.header}
                                        </TableHeaderCell>
                                    ))}
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                {data.map((r, i) => (
                                    <TableRow key={i}>
                                        {props.columns.map((c, j) => (
                                            <TableCell key={j}>{c.value(r)}</TableCell>
                                        ))}
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    ) : null}
                    {totalCount !== null && totalCount > 0 ? (
                        <Pagination currentPage={page} totalItems={totalCount} pageSize={pageSize} onPageChange={setPage} />
                    ) : null}
                </>
            ) : (
                'Loading...'
            )}
        </div>
    );
}
