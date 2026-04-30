import { t } from 'i18next';
import Breadcrumbs from '../../components/Breadcrumbs';
import MainContent from '../../components/MainContent';
import PageHeader from '../../components/PageHeader';
import { Link, useParams } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { formatDateTime, runSparqlQuery, SparqlQuery } from './tools';
import GridRow from '../../components/GridRow';
import GridColumn from '../../components/GridColumn';
import DatasetsCountPanel from './DatasetsCountPanel';
import { DecimalColumn, DurationColumn, HarvestationResult, LiteralColumn, UriColumn } from './types';
import ExternalSourcesPanel from './ExternalSourcesPanel';
import IdentifiedProblems from './IdentifiedProblems';
import ExternalLink from './ExternalLink';
import { useEndpointUrl } from '../../client';

const title = 'Harvestácia lokálneho katalógu';

type Row = {
    duration: DurationColumn;
    averageDuration: DurationColumn;
    successRatio: DecimalColumn;
    name: LiteralColumn;
    catalog: UriColumn;
    parent: UriColumn;
    endpoint: UriColumn;
} & HarvestationResult;

const query: SparqlQuery = {
    prefixes: [
        'm: <https://data.gov.sk/custom/monitoring/>',
        'dcat: <http://www.w3.org/ns/dcat#>',
        'dct: <http://purl.org/dc/terms/>',
        'foaf: <http://xmlns.com/foaf/0.1/>'
    ],
    select: [
        '?id',
        '?publisher',
        '?catalog',
        '?started',
        '?parent',
        '?name',
        '?endpoint',
        '?datasetsCount',
        '?newDatasetsCount',
        '?modifiedDatasetsCount',
        '?removedDatasetsCount',
        '?duration',
        '?averageDuration',
        '?externalSourcesCount',
        '?invalidSourcesCount',
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
            m:externalSourcesCount ?externalSourcesCount ;
            m:invalidExternalSourcesCount ?invalidSourcesCount ;
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
    ]
};

export default function CatalogHarvestationDetail() {
    const { id } = useParams();
    const [row, setRow] = useState<Row | null>(null);

    const endpointUrl = useEndpointUrl();

    useEffect(() => {
        async function fetchData() {
            const effectiveQuery = { ...query, where: [...(query.where ?? [])] };
            effectiveQuery.where.push(`FILTER(?id = <${id}>)`);
            const data = await runSparqlQuery<Row>(endpointUrl, effectiveQuery, 1);
            if (data.length > 0) {
                setRow(data[0]);
            }
        }

        fetchData();
    }, [id, endpointUrl]);

    let breadcrumbs = [{ title: t('nkod'), link: '/' }, { title: 'Harvestácie', link: '/harvestacie' }, { title }];
    if (row?.started?.value && row.parent?.value) {
        breadcrumbs = [
            ...breadcrumbs.slice(0, -1),
            { title: formatDateTime(row.started.value), link: '/harvestacie/detail/' + encodeURIComponent(row.parent.value) },
            { title: row.name?.value }
        ];
    }

    return (
        <>
            <Breadcrumbs items={breadcrumbs} />
            <MainContent>
                <PageHeader>{title}</PageHeader>
                {row ? (
                    <>
                        <GridRow>
                            <GridColumn widthUnits={1} totalUnits={4}>
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Katalóg</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Link to={'/harvestacie/katalog/' + encodeURIComponent(row.catalog.value)} className="idsk-card-title govuk-link">
                                            {row.name?.value}
                                        </Link>
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
                            </GridColumn>
                            <GridColumn widthUnits={1} totalUnits={4}>
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Dátum a čas harvestácie</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Link to={'/harvestacie/detail/' + encodeURIComponent(row.parent.value)} className="idsk-card-title govuk-link">
                                            {formatDateTime(row.started?.value)}
                                        </Link>
                                    </div>
                                </div>
                            </GridColumn>
                        </GridRow>
                        <DatasetsCountPanel {...row} />
                        <ExternalSourcesPanel
                            title="Zdroje"
                            count={row.externalSourcesCount}
                            totalDuration={row.duration}
                            averageDuration={row.averageDuration}
                            successRate={row.successRatio}
                        />
                        <IdentifiedProblems parent={id} />
                    </>
                ) : null}
            </MainContent>
        </>
    );
}
