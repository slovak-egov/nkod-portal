import { t } from 'i18next';
import Breadcrumbs from '../../components/Breadcrumbs';
import MainContent from '../../components/MainContent';
import PageHeader from '../../components/PageHeader';
import { useParams } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { convertXsdDurationToSeconds, formatDateTime, formatSecondsToHms, runSparqlQuery, SparqlQuery } from './tools';
import GridRow from '../../components/GridRow';
import GridColumn from '../../components/GridColumn';
import DatasetsCountPanel from './DatasetsCountPanel';
import { HarvestationResult } from './types';
import ExternalSourcesPanel from './ExternalSourcesPanel';
import LocalCatalogs from './LocalCatalogsTable';
import Publishers from './PublishersTable';
import UnavailableDistributions from './UnavailableDistributions';
import { useEndpointUrl } from '../../client';

const title = 'Harvestácia';

type Row = HarvestationResult;

const query: SparqlQuery = {
    prefixes: ['m: <https://data.gov.sk/custom/monitoring/>'],
    select: [
        '?h',
        '?started',
        '?ended',
        '?duration',
        '?datasetsCount',
        '?newDatasetsCount',
        '?modifiedDatasetsCount',
        '?removedDatasetsCount',
        '?catalogsCount',
        '?catalogSourcesDuration',
        '?catalogSourcesAverageDuration',
        '?catalogSuccessRatio',
        '?catalogSourcesCount',
        '?correctlyHarvestedCatalogsCount',
        '?unavailableCatalogSourcesCount',
        '?publishersCount'
    ],
    where: [
        `?h a m:HarvestationResult ;
         m:started ?started ;
         m:ended ?ended ;
         m:duration ?duration ;
         m:datasetsCount ?datasetsCount ;
         m:newDatasetsCount ?newDatasetsCount ;
         m:modifiedDatasetsCount ?modifiedDatasetsCount ;
         m:removedDatasetsCount ?removedDatasetsCount ;
         m:catalogSourcesDuration ?catalogSourcesDuration ;
         m:catalogSourcesAverageDuration ?catalogSourcesAverageDuration ;
         m:catalogSuccessRatio ?catalogSuccessRatio ;
         m:catalogSourcesCount ?catalogSourcesCount ;
         m:catalogCount ?catalogsCount ;
         m:publisherCount ?publishersCount ;
         m:correctlyHarvestedCatalogCount ?correctlyHarvestedCatalogsCount ;
         m:unavaiableCatalogSourcesCount ?unavailableCatalogSourcesCount .`
    ]
};

export default function HarvestationDetail() {
    const { id } = useParams();
    const [row, setRow] = useState<Row | null>(null);

    const endpointUrl = useEndpointUrl();

    useEffect(() => {
        async function fetchData() {
            const effectiveQuery = { ...query, where: [...(query.where ?? [])] };
            effectiveQuery.where.push(`FILTER(?h = <${id}>)`);
            const data = await runSparqlQuery<Row>(endpointUrl, effectiveQuery, 1);
            if (data.length > 0) {
                setRow(data[0]);
            }
        }

        fetchData();
    }, [id, endpointUrl]);

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: 'Harvestácie', link: '/harvestacie' }, { title }]} />
            <MainContent>
                <PageHeader>
                    {title} {row ? formatDateTime(row.started?.value) : ''}
                </PageHeader>
                {row ? (
                    <>
                        <GridRow>
                            <GridColumn widthUnits={1} totalUnits={4}>
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Trvanie</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        {formatSecondsToHms(convertXsdDurationToSeconds(row.duration?.value))}
                                    </div>
                                </div>
                            </GridColumn>
                            <GridColumn widthUnits={1} totalUnits={4}>
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Začiatok</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        {formatDateTime(row.started?.value)}
                                    </div>
                                </div>
                            </GridColumn>
                            <GridColumn widthUnits={1} totalUnits={4}>
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Koniec</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        {formatDateTime(row.ended?.value)}
                                    </div>
                                </div>
                            </GridColumn>
                        </GridRow>
                        <DatasetsCountPanel {...row} />
                        <ExternalSourcesPanel
                            title="Externé zdroje z lokálných katalógov"
                            count={row.catalogSourcesCount}
                            totalDuration={row.catalogSourcesDuration}
                            averageDuration={row.catalogSourcesAverageDuration}
                            successRate={row.catalogSuccessRatio}
                        />
                        <LocalCatalogs parent={id} />
                        <Publishers parent={id} />
                        <UnavailableDistributions parent={id} />
                    </>
                ) : null}
            </MainContent>
        </>
    );
}
