import { useEffect, useState } from 'react';
import { formatDate, formatDateTime, formatNumber, runSparqlQuery, SparqlQuery } from './tools';
import { DateTimeColumn, IntColumn } from './types';
import { useEndpointUrl } from '../../client';
import { Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import PageSubheader from '../../components/PageSubHeader';

type Props = {
    parent?: string | null | undefined;
};

type DataPoint = {
    date: string;
    count: number;
};

type RawRow = {
    started?: DateTimeColumn | null | undefined;
    datasetsCount?: IntColumn | null | undefined;
};

const genericQuery: SparqlQuery = {
    prefixes: ['m: <https://data.gov.sk/custom/monitoring/>'],
    select: ['?started', '?datasetsCount'],
    where: [
        `?id a m:HarvestationResult ;
         m:context ?context ;
         m:datasetsCount ?datasetsCount .`
    ],
    defaultOrderBy: [{ propertyName: '?started', ascending: true }]
};

export default function DatasetsChart(props: Props) {
    const [data, setData] = useState<DataPoint[] | null>(null);

    const { parent } = props;

    const endpointUrl = useEndpointUrl();

    useEffect(() => {
        async function fetchData() {
            if (!endpointUrl) return;

            const query = { ...genericQuery, where: [...(genericQuery.where ?? [])] };
            if (parent) {
                query.where.push(`
                     ?main a m:HarvestationResult ;
                        m:started ?started ;
                        m:hasSubResult ?id .
                    FILTER(?context = <${parent}>)`);
            } else {
                query.where.push(`?id m:started ?started ;
                    m:context m:all .`);
            }

            const rawData = await runSparqlQuery<RawRow>(endpointUrl, query);
            const data: DataPoint[] = [];
            for (const row of rawData) {
                const date = row.started?.value;
                const count = row.datasetsCount?.value ?? '0';
                if (date) {
                    data.push({ date, count: parseInt(count) });
                }
            }
            setData(data);
        }

        fetchData();
    }, [parent, endpointUrl]);

    return (
        <>
            {data ? (
                <>
                    <div className="govuk-body" style={{ marginBottom: '4em' }}>
                        <PageSubheader>Harvestované datasety</PageSubheader>
                        <ResponsiveContainer height={300} width="100%">
                            <LineChart data={data}>
                                <XAxis dataKey="date" stroke="#626a6e" tickFormatter={(dateStr) => formatDate(dateStr)} />
                                <YAxis stroke="#626a6e" domain={[(dataMin: number) => Math.max(dataMin - 100, 0), (dataMax: number) => dataMax + 100]} />
                                <Line type="monotone" dataKey="count" stroke="#003078" strokeWidth={3} name="Počet datasetov" isAnimationActive={false} />
                                <Tooltip
                                    label={'aaa'}
                                    formatter={(value: number) => formatNumber(value)}
                                    labelFormatter={(dateStr) => formatDateTime(dateStr)}
                                />
                            </LineChart>
                        </ResponsiveContainer>
                    </div>
                </>
            ) : null}
        </>
    );
}
