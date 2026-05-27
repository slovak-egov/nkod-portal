import { t } from 'i18next';
import Breadcrumbs from '../../components/Breadcrumbs';
import MainContent from '../../components/MainContent';
import PageHeader from '../../components/PageHeader';
import SparqlEnabledTable from './SparqlEnabledTable';
import { convertXsdDurationToSeconds, formatDateTime, formatNumber, formatSecondsToHms, getNumber, SparqlQuery } from './tools';
import { Link } from 'react-router-dom';
import { HarvestationResult, UriColumn } from './types';
import DatasetsChart from './DatasetsChart';

const title = 'Harvestácie';

type Row = {
    id: UriColumn;
} & HarvestationResult;

const query: SparqlQuery = {
    prefixes: ['m: <https://data.gov.sk/custom/monitoring/>'],
    select: [
        '?id',
        '?started',
        '?ended',
        '?duration',
        '?datasetsCount',
        '?catalogsCount',
        '?correctlyHarvestedCatalogsCount',
        '?unavailableCatalogSourcesCount',
        '?publishersCount'
    ],
    where: [
        `?id a m:HarvestationResult ;
         m:started ?started ;
         m:ended ?ended ;
         m:duration ?duration ;
         m:datasetsCount ?datasetsCount ;
         m:catalogCount ?catalogsCount ;
         m:publisherCount ?publishersCount ;
         m:correctlyHarvestedCatalogCount ?correctlyHarvestedCatalogsCount ;
         m:unavaiableCatalogSourcesCount ?unavailableCatalogSourcesCount .`
    ],
    defaultOrderBy: [{ propertyName: '?started', ascending: false }]
};

export default function Harvestations() {
    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title }]} />
            <MainContent>
                <PageHeader>{title}</PageHeader>
                <DatasetsChart />
                <SparqlEnabledTable<Row>
                    query={query}
                    header="História"
                    columns={[
                        {
                            header: 'Začiatok',
                            value: (row) => (
                                <Link to={'/harvestacie/detail/' + encodeURIComponent(row.id.value)} className="idsk-card-title govuk-link">
                                    {formatDateTime(row.started?.value)}
                                </Link>
                            ),
                            sortProperty: '?started'
                        },
                        {
                            header: 'Trvanie',
                            value: (row) => formatSecondsToHms(convertXsdDurationToSeconds(row.duration?.value)),
                            sortProperty: '?duration'
                        },
                        {
                            header: 'Harvestované datasety',
                            value: (row) => <div style={{ textAlign: 'center' }}>{formatNumber(row.datasetsCount?.value)}</div>,
                            sortProperty: '?datasetsCount'
                        },
                        {
                            header: 'Harvestované LKODy',
                            value: (row) => (
                                <div style={{ textAlign: 'center' }}>
                                    {formatNumber(row.correctlyHarvestedCatalogsCount?.value)} / {formatNumber(row.catalogsCount?.value)}
                                </div>
                            ),
                            sortProperty: '?catalogsCount'
                        },
                        {
                            header: 'Nedostupné zdroje',
                            value: (row) => (
                                <div style={{ textAlign: 'center' }}>
                                    {((getNumber(row.unavailableCatalogSourcesCount?.value) ?? 0) > 0 ? '🔴' : '🟢') +
                                        ' ' +
                                        formatNumber(row.unavailableCatalogSourcesCount?.value)}
                                </div>
                            ),
                            sortProperty: '?unavailableCatalogSourcesCount'
                        },
                        {
                            header: 'Poskytovatelia',
                            value: (row) => <div style={{ textAlign: 'center' }}>{formatNumber(row.publishersCount?.value)}</div>,
                            sortProperty: '?publishersCount'
                        }
                    ]}
                    exportDefintion={{
                        columns: [
                            { header: 'Začiatok', value: (row) => formatDateTime(row.started?.value) },
                            { header: 'Trvanie', value: (row) => formatSecondsToHms(convertXsdDurationToSeconds(row.duration?.value)) },
                            { header: 'Harvestované datasety', value: (row) => row.datasetsCount?.value ?? '' },
                            {
                                header: 'Harvestované LKODy',
                                value: (row) => (row.correctlyHarvestedCatalogsCount?.value ?? '') + ' / ' + (row.catalogsCount?.value ?? '')
                            },
                            { header: 'Nedostupné zdroje', value: (row) => row.unavailableCatalogSourcesCount?.value ?? '' },
                            { header: 'Počet poskytovateľov', value: (row) => row.publishersCount?.value ?? '' }
                        ]
                    }}
                />
            </MainContent>
        </>
    );
}
