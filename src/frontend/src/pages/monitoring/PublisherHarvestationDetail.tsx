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
import { useEndpointUrl } from '../../client';

const title = 'Harvestácia datasetov poskytovateľa';

type Row = {
    duration: DurationColumn;
    averageDuration: DurationColumn;
    successRatio: DecimalColumn;
    name: LiteralColumn;
    parent: UriColumn;
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
        '?name',
        '?started',
        '?parent',
        '?datasetsCount',
        '?newDatasetsCount',
        '?modifiedDatasetsCount',
        '?removedDatasetsCount',
        '?externalSourcesCount',
        '?duration',
        '?averageDuration',
        '?invalidSourcesCount',
        '?successRatio'
    ],
    where: [
        `?id a m:HarvestationResult ;
            m:context ?p ;
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
            
        ?p a foaf:Agent;
             foaf:name ?name .
             
        FILTER(LANG(?name) = "sk")`
    ]
};

export default function PublisherHarvestationDetail() {
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
                                    <div className="govuk-body nkod-detail-attribute-name">Poskytovateľ</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        {row.name?.value}
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
                            title="Externé zdroje"
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
