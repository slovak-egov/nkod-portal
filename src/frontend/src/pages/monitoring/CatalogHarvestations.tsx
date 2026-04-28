import { t } from 'i18next';
import Breadcrumbs from '../../components/Breadcrumbs';
import MainContent from '../../components/MainContent';
import PageHeader from '../../components/PageHeader';
import SparqlEnabledTable from './SparqlEnabledTable';
import {
    convertXsdDurationToSeconds,
    formatDateTime,
    formatNumber,
    formatSecondsToHms,
    formatSecondsToMilisecondsOrSeconds,
    getNumber,
    runSparqlQuery,
    SparqlQuery
} from './tools';
import { Link, useParams } from 'react-router-dom';
import { DurationColumn, HarvestationResult, IntColumn, LiteralColumn, UriColumn } from './types';
import GridRow from '../../components/GridRow';
import GridColumn from '../../components/GridColumn';
import { useEffect, useState } from 'react';
import ExternalLink from './ExternalLink';
import { useEndpointUrl } from '../../client';

const title = 'Harvestácie lokálneho katalógu';

type Row = {
    id: UriColumn;
    invalidSourcesCount: IntColumn;
    averageDuration: DurationColumn;
} & HarvestationResult;

type CatalogRow = {
    catalogName: LiteralColumn;
    publisher: LiteralColumn;
    endpoint: LiteralColumn;
};

const genericQuery: SparqlQuery = {
    prefixes: [
        'm: <https://data.gov.sk/custom/monitoring/>',
        'dcat: <http://www.w3.org/ns/dcat#>',
        'dct: <http://purl.org/dc/terms/>',
        'foaf: <http://xmlns.com/foaf/0.1/>'
    ],
    select: [
        '?id',
        '?started',
        '?publisher',
        '?catalog',
        '?name',
        '?endpoint',
        '?datasetsCount',
        '?newDatasetsCount',
        '?modifiedDatasetsCount',
        '?removedDatasetsCount',
        '?duration',
        '?averageDuration',
        '?invalidSourcesCount',
        '?problemsCount',
        '?successRatio'
    ],
    where: [
        `?id a m:HarvestationResult ;
               m:context ?catalog ;
               m:datasetsCount ?datasetsCount ;
               m:newDatasetsCount ?newDatasetsCount ;
               m:modifiedDatasetsCount ?modifiedDatasetsCount ;
               m:removedDatasetsCount ?removedDatasetsCount ;
               m:externalSourcesDuration ?duration ;
               m:externalSourcesAverageDuration ?averageDuration ;
               m:invalidExternalSourcesCount ?invalidSourcesCount ;
               m:problemsCount ?problemsCount ;
               m:sucessRatio ?successRatio .

        ?parent a m:HarvestationResult ;
                  m:started ?started ;
                  m:hasSubResult ?id .
    
        ?catalog a dcat:Catalog ;
            dct:publisher ?p ;
            dct:title ?name ;
            dcat:endpointURL ?endpoint .
            
        ?p a foaf:Agent;
             foaf:name ?publisher .
             
        FILTER(LANG(?name) = "sk")
        FILTER(LANG(?publisher) = "sk")`
    ],
    defaultOrderBy: [{ propertyName: '?started', ascending: false }]
};

const genericCatalogQuery: SparqlQuery = {
    prefixes: [
        'm: <https://data.gov.sk/custom/monitoring/>',
        'dcat: <http://www.w3.org/ns/dcat#>',
        'dct: <http://purl.org/dc/terms/>',
        'foaf: <http://xmlns.com/foaf/0.1/>'
    ],
    select: ['?publisher', '?catalogName', '?endpoint'],
    where: [
        `?catalog a dcat:Catalog ;
            dct:publisher ?p ;
            dct:title ?catalogName ;
            dcat:endpointURL ?endpoint .
            
        ?p a foaf:Agent;
             foaf:name ?publisher .
             
        FILTER(LANG(?catalogName) = "sk")
        FILTER(LANG(?publisher) = "sk")`
    ]
};

export default function CatalogHarvestations() {
    const { id } = useParams();
    const query = { ...genericQuery, where: [...(genericQuery.where ?? [])] };
    const [row, setRow] = useState<CatalogRow | null>(null);
    query.where.push(`FILTER(?catalog = <${id}>)`);

    const endpointUrl = useEndpointUrl();

    useEffect(() => {
        async function fetchData() {
            if (!endpointUrl) return;

            const query = { ...genericCatalogQuery, where: [...(genericCatalogQuery.where ?? [])] };
            query.where.push(`FILTER(?catalog = <${id}>)`);
            const data = await runSparqlQuery<CatalogRow>(endpointUrl, query, 1);
            setRow(data[0] ?? null);
        }

        fetchData();
    }, [endpointUrl, id]);

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: 'Harvestácie', link: '/harvestacie' }, { title }]} />
            <MainContent>
                <PageHeader>{title}</PageHeader>
                {row ? (
                    <GridRow>
                        <GridColumn widthUnits={1} totalUnits={4}>
                            <div className="nkod-detail-attribute">
                                <div className="govuk-body nkod-detail-attribute-name">Katalóg</div>
                                <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                    {row.catalogName?.value}
                                </div>
                            </div>
                        </GridColumn>
                        <GridColumn widthUnits={1} totalUnits={4}>
                            <div className="nkod-detail-attribute">
                                <div className="govuk-body nkod-detail-attribute-name">Prístupový bod</div>
                                <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                    <ExternalLink href={row.endpoint?.value} />
                                </div>
                            </div>
                        </GridColumn>{' '}
                        <GridColumn widthUnits={1} totalUnits={4}>
                            <div className="nkod-detail-attribute">
                                <div className="govuk-body nkod-detail-attribute-name">Poskytovateľ</div>
                                <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                    {row.publisher?.value}
                                </div>
                            </div>
                        </GridColumn>
                    </GridRow>
                ) : null}
                <SparqlEnabledTable<Row>
                    query={query}
                    header="História"
                    columns={[
                        {
                            header: 'Začiatok',
                            value: (row) => (
                                <Link to={'/harvestacie/katalog/detail/' + encodeURIComponent(row.id.value)} className="idsk-card-title govuk-link">
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
                            header: 'Datasety',
                            value: (row) => formatNumber(row.datasetsCount?.value),
                            sortProperty: '?datasetsCount'
                        },
                        {
                            header: 'Priemerná doba odozvy',
                            value: (row) => formatSecondsToMilisecondsOrSeconds(convertXsdDurationToSeconds(row.averageDuration?.value)),
                            sortProperty: '?averageDuration'
                        },
                        {
                            header: 'Nedostupné zdroje',
                            value: (row) => (
                                <div style={{ textAlign: 'center' }}>
                                    {((getNumber(row.invalidSourcesCount.value) ?? 0) > 0 ? '🔴' : '🟢') + ' ' + formatNumber(row.invalidSourcesCount?.value)}
                                </div>
                            ),
                            sortProperty: '?invalidSourcesCount'
                        },
                        {
                            header: 'Identifikované problémy',
                            value: (row) => (
                                <div style={{ textAlign: 'center' }}>
                                    {((getNumber(row.problemsCount?.value) ?? 0) > 0 ? '🔴' : '🟢') + ' ' + formatNumber(row.problemsCount?.value)}
                                </div>
                            ),
                            sortProperty: '?problemsCount'
                        }
                    ]}
                    exportDefintion={{
                        columns: [
                            { header: 'Začiatok', value: (row) => formatDateTime(row.started?.value) },
                            { header: 'Trvanie', value: (row) => formatSecondsToHms(convertXsdDurationToSeconds(row.duration?.value)) },
                            { header: 'Datasety', value: (row) => row.datasetsCount?.value ?? '' },
                            { header: 'Nedostupné zdroje', value: (row) => row.invalidSourcesCount?.value ?? '' },
                            { header: 'Identifikované problémy', value: (row) => row.problemsCount?.value ?? '' }
                        ]
                    }}
                />
            </MainContent>
        </>
    );
}
