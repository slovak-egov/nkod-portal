import { useEffect, useState } from 'react';
import { useParams } from 'react-router';
import Breadcrumbs from '../../components/Breadcrumbs';
import { t } from 'i18next';
import MainContent from '../../components/MainContent';
import PageHeader from '../../components/PageHeader';
import PageSubheader from '../../components/PageSubHeader';
import Indicator from './Indicator';
import GridArrangement from './GridArrangement';
import Table from '../../components/Table';
import TableHead from '../../components/TableHead';
import TableHeaderCell from '../../components/TableHeaderCell';
import TableBody from '../../components/TableBody';
import TableRow from '../../components/TableRow';
import TableCell from '../../components/TableCell';
import { runSparqlQuery, SparqlQuery } from '../monitoring/tools';
import { LiteralColumn, UriColumn } from '../monitoring/types';
import { useEndpointUrl } from '../../client';
import PublisherDatasets from './PublisherDatasets';

const defaultQuery: SparqlQuery = {
    prefixes: ['foaf: <http://xmlns.com/foaf/0.1/>', 'dqv: <http://www.w3.org/ns/dqv#>', ': <https://data.gov.sk/def/observation/data-quality/metrics/>'],
    select: ['?id', '?name'],
    where: [
        `?id a foaf:Agent;
             foaf:name ?name .
             
        FILTER(LANG(?name) = "sk")`
    ]
};

function addMeasurement(query: SparqlQuery, variable: string, measurement: string) {
    query.select.push(`?${variable}`);
    query.where ??= [];
    query.where.push(`OPTIONAL { ?id dqv:hasQualityMeasurement [ dqv:isMeasurementOf :${measurement} ; dqv:value ?${variable} ] . }`);
}

addMeasurement(defaultQuery, 'datasetsCount', 'pocetDatasetovPoskytovatela');
addMeasurement(defaultQuery, 'dataSeriesCount', 'pocetDatovychSeriiPoskytovatela');
addMeasurement(defaultQuery, 'requiredMetadataCount', 'pocetSplnenychKontrolNaVyplneniePovinnychMetadat');
addMeasurement(defaultQuery, 'requiredMetadataTotal', 'pocetKontrolNaVyplneniePovinnychMetadat');
addMeasurement(defaultQuery, 'optionalMetadataCount', 'pocetSplnenychKontrolNaVyplnenieOdporucanychMetadat');
addMeasurement(defaultQuery, 'optionalMetadataTotal', 'pocetKontrolNaVyplnenieOdporucanychMetadat');
addMeasurement(defaultQuery, 'datasetsPublicationMinimumCount', 'pocetDatasetovPublikacnehoMinimaPoskytovatela');
addMeasurement(defaultQuery, 'datasetsHvdCount', 'pocetHvdDatasetovPoskytovatela');
addMeasurement(defaultQuery, 'datasetsTopCount', 'pocetNajziadanejsichDatasetovPoskytovatela');

addMeasurement(defaultQuery, 'datasetDescriptionCount', 'pocetSplnenychKontrolPopisDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetDescriptionTotal', 'pocetKontrolPopisDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetThemeCount', 'pocetSplnenychKontrolTemaDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetThemeTotal', 'pocetKontrolTemaDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetPeriodicityCount', 'pocetSplnenychKontrolPeriodicitaAktualizacieDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetPeriodicityTotal', 'pocetKontrolPeriodicitaAktualizacieDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetKeywordCount', 'pocetSplnenychKontrolKlucoveSlovoDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetKeywordTotal', 'pocetKontrolKlucoveSlovoDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetSpatialCount', 'pocetSplnenychKontrolUzemnePokrytieDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetSpatialTotal', 'pocetKontrolUzemnePokrytieDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetDownloadLinkCount', 'pocetSplnenychKontrolOdkazNaStiahnutieDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetDownloadLinkTotal', 'pocetKontrolOdkazNaStiahnutieDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetLicenseCount', 'pocetSplnenychKontrolVyplnenaLicenciaDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetLicenseTotal', 'pocetKontrolVyplnenaLicenciaDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetFormatCount', 'pocetSplnenychKontrolVyplnenyFormatDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetFormatTotal', 'pocetKontrolVyplnenyFormatDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetTypMediaCount', 'pocetSplnenychKontrolVyplnenyTypMediaDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetTypMediaTotal', 'pocetKontrolVyplnenyTypMediaDatasetuPoskytovatela');

addMeasurement(defaultQuery, 'datasetLegislationCount', 'pocetSplnenychKontrolVyplnenyPravnyPredpisHvdDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetLegislationTotal', 'pocetKontrolVyplnenyPravnyPredpisHvdDatasetuPoskytovatela');

addMeasurement(defaultQuery, 'datasetHvdCategoryCount', 'pocetSplnenychKontrolVyplnenaKategoriaHvdDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetHvdCategoryTotal', 'pocetKontrolVyplnenaKategoriaHvdDatasetuPoskytovatela');

addMeasurement(defaultQuery, 'distributionLegislationCount', 'pocetSplnenychKontrolVyplnenyPravnyPredpisHvdDistribuciePoskytovatela');
addMeasurement(defaultQuery, 'distributionLegislationTotal', 'pocetKontrolVyplnenyPravnyPredpisHvdDistribuciePoskytovatela');

addMeasurement(defaultQuery, 'datasetHvdDataServiceCount', 'pocetSplnenychKontrolVyplnenaDatovaSluzbaHvdDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetHvdDataServiceTotal', 'pocetKontrolVyplnenaDatovaSluzbaHvdDatasetuPoskytovatela');

addMeasurement(defaultQuery, 'datasetHvdDataServiceNameCount', 'pocetSplnenychKontrolVyplnenyNazovDatovejSluzbyHvdDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetHvdDataServiceNameTotal', 'pocetKontrolVyplnenyNazovDatovejSluzbyHvdDatasetuPoskytovatela');

addMeasurement(defaultQuery, 'datasetHvdDataServiceEndpointCount', 'pocetSplnenychKontrolVyplnenyPristupovyBodDatovejSluzbyHvdDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetHvdDataServiceEndpointTotal', 'pocetKontrolVyplnenyPristupovyBodDatovejSluzbyHvdDatasetuPoskytovatela');

addMeasurement(
    defaultQuery,
    'datasetHvdDataServiceDocumentationCount',
    'pocetSplnenychKontrolVyplnenyOdkazNaDokumentaciuDatovejSluzbyHvdDatasetuPoskytovatela'
);
addMeasurement(defaultQuery, 'datasetHvdDataServiceDocumentationTotal', 'pocetKontrolVyplnenyOdkazNaDokumentaciuDatovejSluzbyHvdDatasetuPoskytovatela');

addMeasurement(defaultQuery, 'datasetHvdDataServiceContactCount', 'pocetSplnenychKontrolVyplnenyKontaktnyBodDatovejSluzbyHvdDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetHvdDataServiceContactTotal', 'pocetKontrolVyplnenyKontaktnyBodDatovejSluzbyHvdDatasetuPoskytovatela');

addMeasurement(defaultQuery, 'datasetNonHvdDataServiceNameCount', 'pocetSplnenychKontrolVyplnenyNazovDatovejSluzbyDatasetovMimoHvdPoskytovatela');
addMeasurement(defaultQuery, 'datasetNonHvdDataServiceNameTotal', 'pocetKontrolVyplnenyNazovDatovejSluzbyDatasetovMimoHvdPoskytovatela');

addMeasurement(defaultQuery, 'datasetNonHvdDataServiceEndpointCount', 'pocetSplnenychKontrolVyplnenyPristupovyBodDatovejSluzbyDatasetovMimoHvdPoskytovatela');
addMeasurement(defaultQuery, 'datasetNonHvdDataServiceEndpointTotal', 'pocetKontrolVyplnenyPristupovyBodDatovejSluzbyDatasetovMimoHvdPoskytovatela');

addMeasurement(defaultQuery, 'datasetTemporalCount', 'pocetSplnenychKontrolVyplneneCasovePokrytieDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetTemporalTotal', 'pocetKontrolVyplneneCasovePokrytieDatasetuPoskytovatela');

addMeasurement(defaultQuery, 'datasetContactCount', 'pocetSplnenychKontrolVyplnenyKontaktnyBodDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetContactTotal', 'pocetKontrolVyplnenyKontaktnyBodDatasetuPoskytovatela');

addMeasurement(defaultQuery, 'datasetIssuedCount', 'pocetSplnenychKontrolVyplnenyDatumVytvoreniaDatasetuPoskytovatela');
addMeasurement(defaultQuery, 'datasetIssuedTotal', 'pocetKontrolVyplnenyDatumVytvoreniaDatasetuPoskytovatela');

addMeasurement(
    defaultQuery,
    'datasetNonHvdDataServiceDocumentationCount',
    'pocetSplnenychKontrolVyplnenyOdkazNaDokumentaciuDatovejSluzbyDatasetuMimoHvdPoskytovatela'
);
addMeasurement(defaultQuery, 'datasetNonHvdDataServiceDocumentationTotal', 'pocetKontrolVyplnenyOdkazNaDokumentaciuDatovejSluzbyDatasetuMimoHvdPoskytovatela');

addMeasurement(defaultQuery, 'datasetNonHvdDataServiceContactCount', 'pocetSplnenychKontrolVyplnenyKontaktnyBodDatovejSluzbyDatasetuMimoHvdPoskytovatela');
addMeasurement(defaultQuery, 'datasetNonHvdDataServiceContactTotal', 'pocetKontrolVyplnenyKontaktnyBodDatovejSluzbyDatasetuMimoHvdPoskytovatela');

addMeasurement(defaultQuery, 'datasetQuality1Count', 'pocetDatasetovSKvalitou1');
addMeasurement(defaultQuery, 'datasetQuality2Count', 'pocetDatasetovSKvalitou2');
addMeasurement(defaultQuery, 'datasetQuality3Count', 'pocetDatasetovSKvalitou3');
addMeasurement(defaultQuery, 'datasetQuality4Count', 'pocetDatasetovSKvalitou4');
addMeasurement(defaultQuery, 'datasetQuality5Count', 'pocetDatasetovSKvalitou5');
addMeasurement(defaultQuery, 'datasetLicenseOpenCount', 'pocetDatasetovSOtvorenouLicenciou');
addMeasurement(defaultQuery, 'datasetLicenseTotalCount', 'pocetDatasetovSLicenciou');

addMeasurement(defaultQuery, 'availableDownloadLinkCount', 'pocetSplnenychKontrolMetrikaDostupnostiDownloadURL');
addMeasurement(defaultQuery, 'availableDownloadLinkTotal', 'pocetKontrolMetrikaDostupnostiDownloadURL');

addMeasurement(defaultQuery, 'availableSchemaLinkCount', 'pocetSplnenychKontrolMetrikaDostupnostiSchematu');
addMeasurement(defaultQuery, 'availableSchemaLinkTotal', 'pocetKontrolMetrikaDostupnostiSchematu');

addMeasurement(defaultQuery, 'availableDocumentationLinkCount', 'pocetSplnenychKontrolMetrikaDostupnostiDokumentace');
addMeasurement(defaultQuery, 'availableDocumentationLinkTotal', 'pocetKontrolMetrikaDostupnostiDokumentace');

addMeasurement(defaultQuery, 'availableDataServiceEndpointCount', 'pocetSplnenychKontrolMetrikaDostupnostiEndpointURL');
addMeasurement(defaultQuery, 'availableDataServiceEndpointTotal', 'pocetKontrolMetrikaDostupnostiEndpointURL');

addMeasurement(defaultQuery, 'availableDataServiceDescriptionCount', 'pocetSplnenychKontrolMetrikaDostupnostiEndpointDescription');
addMeasurement(defaultQuery, 'availableDataServiceDescriptionTotal', 'pocetKontrolMetrikaDostupnostiEndpointDescription');

addMeasurement(defaultQuery, 'matchContentTypeCount', 'pocetSplnenychKontrolMetrikaSpravnostiMediaTypu');
addMeasurement(defaultQuery, 'matchContentTypeTotal', 'pocetKontrolMetrikaSpravnostiMediaTypu');

addMeasurement(defaultQuery, 'corsEnabledDownloadLinkCount', 'pocetSplnenychKontrolMetrikaDostupnostiCORSDownloadURL');
addMeasurement(defaultQuery, 'corsEnabledDownloadLinkTotal', 'pocetKontrolMetrikaDostupnostiCORSDownloadURL');

addMeasurement(defaultQuery, 'corsEnabledSchemaLinkCount', 'pocetSplnenychKontrolMetrikaDostupnostiCORSSchematu');
addMeasurement(defaultQuery, 'corsEnabledSchemaLinkTotal', 'pocetKontrolMetrikaDostupnostiCORSSchematu');

addMeasurement(defaultQuery, 'corsEnabledDataServiceEndpointCount', 'pocetSplnenychKontrolMetrikaDostupnostiCORSEndpointURL');
addMeasurement(defaultQuery, 'corsEnabledDataServiceEndpointTotal', 'pocetKontrolMetrikaDostupnostiCORSEndpointURL');

addMeasurement(defaultQuery, 'corsEnabledDataServiceDescriptionCount', 'pocetSplnenychKontrolMetrikaDostupnostiCORSEndpointDescription');
addMeasurement(defaultQuery, 'corsEnabledDataServiceDescriptionTotal', 'pocetKontrolMetrikaDostupnostiCORSEndpointDescription');

type Row = {
    id?: UriColumn | null | undefined;
    name?: LiteralColumn | null | undefined;
    datasetsCount?: LiteralColumn | null | undefined;
    dataSeriesCount?: LiteralColumn | null | undefined;

    requiredMetadataCount?: LiteralColumn | null | undefined;
    requiredMetadataTotal?: LiteralColumn | null | undefined;

    optionalMetadataCount?: LiteralColumn | null | undefined;
    optionalMetadataTotal?: LiteralColumn | null | undefined;

    datasetsPublicationMinimumCount?: LiteralColumn | null | undefined;
    datasetsHvdCount?: LiteralColumn | null | undefined;
    datasetsTopCount?: LiteralColumn | null | undefined;

    datasetDescriptionCount?: LiteralColumn | null | undefined;
    datasetDescriptionTotal?: LiteralColumn | null | undefined;

    datasetThemeCount?: LiteralColumn | null | undefined;
    datasetThemeTotal?: LiteralColumn | null | undefined;

    datasetPeriodicityCount?: LiteralColumn | null | undefined;
    datasetPeriodicityTotal?: LiteralColumn | null | undefined;

    datasetKeywordCount?: LiteralColumn | null | undefined;
    datasetKeywordTotal?: LiteralColumn | null | undefined;

    datasetDownloadLinkCount?: LiteralColumn | null | undefined;
    datasetDownloadLinkTotal?: LiteralColumn | null | undefined;

    datasetSpatialCount?: LiteralColumn | null | undefined;
    datasetSpatialTotal?: LiteralColumn | null | undefined;

    datasetFormatCount?: LiteralColumn | null | undefined;
    datasetFormatTotal?: LiteralColumn | null | undefined;

    datasetTypMediaCount?: LiteralColumn | null | undefined;
    datasetTypMediaTotal?: LiteralColumn | null | undefined;

    datasetLicenseCount?: LiteralColumn | null | undefined;
    datasetLicenseTotal?: LiteralColumn | null | undefined;

    datasetLegislationCount?: LiteralColumn | null | undefined;
    datasetLegislationTotal?: LiteralColumn | null | undefined;

    datasetHvdCategoryCount?: LiteralColumn | null | undefined;
    datasetHvdCategoryTotal?: LiteralColumn | null | undefined;

    distributionLegislationCount?: LiteralColumn | null | undefined;
    distributionLegislationTotal?: LiteralColumn | null | undefined;

    datasetHvdDataServiceCount?: LiteralColumn | null | undefined;
    datasetHvdDataServiceTotal?: LiteralColumn | null | undefined;

    datasetHvdDataServiceNameCount?: LiteralColumn | null | undefined;
    datasetHvdDataServiceNameTotal?: LiteralColumn | null | undefined;

    datasetHvdDataServiceEndpointCount?: LiteralColumn | null | undefined;
    datasetHvdDataServiceEndpointTotal?: LiteralColumn | null | undefined;

    datasetHvdDataServiceDocumentationCount?: LiteralColumn | null | undefined;
    datasetHvdDataServiceDocumentationTotal?: LiteralColumn | null | undefined;

    datasetHvdDataServiceContactCount?: LiteralColumn | null | undefined;
    datasetHvdDataServiceContactTotal?: LiteralColumn | null | undefined;

    datasetNonHvdDataServiceNameCount?: LiteralColumn | null | undefined;
    datasetNonHvdDataServiceNameTotal?: LiteralColumn | null | undefined;

    datasetNonHvdDataServiceEndpointCount?: LiteralColumn | null | undefined;
    datasetNonHvdDataServiceEndpointTotal?: LiteralColumn | null | undefined;

    datasetTemporalCount?: LiteralColumn | null | undefined;
    datasetTemporalTotal?: LiteralColumn | null | undefined;

    datasetContactCount?: LiteralColumn | null | undefined;
    datasetContactTotal?: LiteralColumn | null | undefined;

    datasetIssuedCount?: LiteralColumn | null | undefined;
    datasetIssuedTotal?: LiteralColumn | null | undefined;

    datasetNonHvdDataServiceDocumentationCount?: LiteralColumn | null | undefined;
    datasetNonHvdDataServiceDocumentationTotal?: LiteralColumn | null | undefined;

    datasetNonHvdDataServiceContactCount?: LiteralColumn | null | undefined;
    datasetNonHvdDataServiceContactTotal?: LiteralColumn | null | undefined;

    availableDownloadLinkCount?: LiteralColumn | null | undefined;
    availableDownloadLinkTotal?: LiteralColumn | null | undefined;

    availableSchemaLinkCount?: LiteralColumn | null | undefined;
    availableSchemaLinkTotal?: LiteralColumn | null | undefined;

    availableDocumentationLinkCount?: LiteralColumn | null | undefined;
    availableDocumentationLinkTotal?: LiteralColumn | null | undefined;

    availableDataServiceEndpointCount?: LiteralColumn | null | undefined;
    availableDataServiceEndpointTotal?: LiteralColumn | null | undefined;

    availableDataServiceDescriptionCount?: LiteralColumn | null | undefined;
    availableDataServiceDescriptionTotal?: LiteralColumn | null | undefined;

    matchContentTypeCount?: LiteralColumn | null | undefined;
    matchContentTypeTotal?: LiteralColumn | null | undefined;

    corsEnabledDownloadLinkCount?: LiteralColumn | null | undefined;
    corsEnabledDownloadLinkTotal?: LiteralColumn | null | undefined;

    corsEnabledSchemaLinkCount?: LiteralColumn | null | undefined;
    corsEnabledSchemaLinkTotal?: LiteralColumn | null | undefined;

    corsEnabledDataServiceEndpointCount?: LiteralColumn | null | undefined;
    corsEnabledDataServiceEndpointTotal?: LiteralColumn | null | undefined;

    corsEnabledDataServiceDescriptionCount?: LiteralColumn | null | undefined;
    corsEnabledDataServiceDescriptionTotal?: LiteralColumn | null | undefined;

    datasetQuality1Count?: LiteralColumn | null | undefined;
    datasetQuality2Count?: LiteralColumn | null | undefined;
    datasetQuality3Count?: LiteralColumn | null | undefined;
    datasetQuality4Count?: LiteralColumn | null | undefined;
    datasetQuality5Count?: LiteralColumn | null | undefined;

    datasetLicenseOpenCount?: LiteralColumn | null | undefined;
    datasetLicenseTotalCount?: LiteralColumn | null | undefined;
};

function asNumber(column?: LiteralColumn | null | undefined): number {
    if (column !== null && column !== undefined) {
        const n = Number(column.value);
        if (!isNaN(n)) {
            return n;
        }
    }
    return 0;
}

const title = 'Kvalita metadát a datasetov poskytovateľa';

export default function PublisherDataQualityDetail() {
    const { id } = useParams();
    const [row, setRow] = useState<Row | null>();

    const query = { ...defaultQuery, where: [...(defaultQuery.where ?? [])] };

    const endpointUrl = useEndpointUrl();

    useEffect(() => {
        let isCurrent = true;
        setRow(null);

        async function fetchData() {
            const effectiveQuery = { ...query, where: [...(query.where ?? [])] };
            effectiveQuery.where.push(`FILTER(?id = <${id}>)`);
            const data = await runSparqlQuery<Row>(endpointUrl, effectiveQuery, 1);
            if (isCurrent) {
                setRow(data.length > 0 ? data[0] : null);
            }
        }

        fetchData();

        return () => {
            isCurrent = false;
        };
    }, [id, endpointUrl]);

    const useDatasetCount = Math.max(asNumber(row?.datasetsCount), 0);
    const useDataseriesCount = Math.max(asNumber(row?.dataSeriesCount), 0);
    const pureDatasetCount = Math.max(useDatasetCount - useDataseriesCount, 0);

    const datasetQualityCount = [];
    for (let i = 1; i <= 5; i++) {
        datasetQualityCount.push(Math.max(asNumber(row?.[`datasetQuality${i}Count` as keyof Row] as LiteralColumn | null | undefined), 0));
    }

    const datasetQualitySum = datasetQualityCount[2] + datasetQualityCount[3] + datasetQualityCount[4];
    const datasetQualitySumAll = datasetQualityCount[0] + datasetQualityCount[1] + datasetQualityCount[2] + datasetQualityCount[3] + datasetQualityCount[4];

    const availabilityCount =
        asNumber(row?.availableDownloadLinkCount) +
        asNumber(row?.availableSchemaLinkCount) +
        asNumber(row?.availableDocumentationLinkCount) +
        asNumber(row?.availableDataServiceEndpointCount) +
        asNumber(row?.availableDataServiceDescriptionCount) +
        asNumber(row?.corsEnabledDownloadLinkCount) +
        asNumber(row?.corsEnabledSchemaLinkCount) +
        asNumber(row?.corsEnabledDataServiceEndpointCount) +
        asNumber(row?.corsEnabledDataServiceDescriptionCount);

    const availabilityTotal =
        asNumber(row?.availableDownloadLinkTotal) +
        asNumber(row?.availableSchemaLinkTotal) +
        asNumber(row?.availableDocumentationLinkTotal) +
        asNumber(row?.availableDataServiceEndpointTotal) +
        asNumber(row?.availableDataServiceDescriptionTotal) +
        asNumber(row?.corsEnabledDownloadLinkTotal) +
        asNumber(row?.corsEnabledSchemaLinkTotal) +
        asNumber(row?.corsEnabledDataServiceEndpointTotal) +
        asNumber(row?.corsEnabledDataServiceDescriptionTotal);

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: 'Kvalita metadát', link: '/kvalita-metadat' }, { title }]} />
            <MainContent>
                {row && id ? (
                    <>
                        <PageHeader>{row.name?.value}</PageHeader>
                        <GridArrangement
                            columns={[
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Počet datasetov</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        {asNumber(row.datasetsCount)}
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Počet dátových sérií</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        {asNumber(row.dataSeriesCount)}
                                    </div>
                                </div>
                            ]}
                        />
                        <PageSubheader>Súhrnné hodnotenie</PageSubheader>
                        <GridArrangement
                            columns={[
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnenie povinných metadát</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={asNumber(row.requiredMetadataCount)} denominator={asNumber(row.requiredMetadataTotal)} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnenie odporúčaných metadát</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={asNumber(row.optionalMetadataCount)} denominator={asNumber(row.optionalMetadataTotal)} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Dostupnosť datasetov</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={availabilityCount} denominator={availabilityTotal} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Kvalita datasetov</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={datasetQualitySum} denominator={pureDatasetCount} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Otvorenosť licencií</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={asNumber(row.datasetLicenseOpenCount)} denominator={pureDatasetCount} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Počet datasetov Publikačného minima</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        {asNumber(row.datasetsPublicationMinimumCount)}
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Počet HVD datasetov</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        {asNumber(row.datasetsHvdCount)}
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Počet najžiadanejších datasetov</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        {asNumber(row.datasetsTopCount)}
                                    </div>
                                </div>
                            ]}
                        />
                        <PageSubheader>Vyplnenie povinných metadát - datasety</PageSubheader>
                        <GridArrangement
                            columns={[
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnený popis</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={asNumber(row.datasetDescriptionCount)} denominator={asNumber(row.datasetDescriptionTotal)} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnená téma</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={asNumber(row.datasetThemeCount)} denominator={asNumber(row.datasetThemeTotal)} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnená periodicita aktualizácie</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={asNumber(row.datasetPeriodicityCount)} denominator={asNumber(row.datasetPeriodicityTotal)} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnené aspoň 1 kľúčové slovo</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={asNumber(row.datasetKeywordCount)} denominator={asNumber(row.datasetKeywordTotal)} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnený odkaz na stiahnutie</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={asNumber(row.datasetDownloadLinkCount)} denominator={asNumber(row.datasetDownloadLinkTotal)} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnené geografické pokrytie</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={asNumber(row.datasetSpatialCount)} denominator={asNumber(row.datasetSpatialTotal)} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnený formát</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={asNumber(row.datasetFormatCount)} denominator={asNumber(row.datasetFormatTotal)} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnený typ média</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={asNumber(row.datasetTypMediaCount)} denominator={asNumber(row.datasetTypMediaTotal)} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnená licencia</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={asNumber(row.datasetLicenseCount)} denominator={asNumber(row.datasetLicenseTotal)} />
                                    </div>
                                </div>
                            ]}
                        />
                        <PageSubheader>Vyplnenie povinných metadát - HVD datasety</PageSubheader>
                        <GridArrangement
                            columns={[
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnený právny predpis datasetu</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={asNumber(row.datasetLegislationCount)} denominator={asNumber(row.datasetLegislationTotal)} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnená HVD kategória</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={asNumber(row.datasetHvdCategoryCount)} denominator={asNumber(row.datasetHvdCategoryTotal)} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnený právny predpis distribúcie</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator
                                            numerator={asNumber(row.distributionLegislationCount)}
                                            denominator={asNumber(row.distributionLegislationTotal)}
                                        />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnená dátová služba</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator
                                            numerator={asNumber(row.datasetHvdDataServiceCount)}
                                            denominator={asNumber(row.datasetHvdDataServiceTotal)}
                                        />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnený názov dátovej služby</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator
                                            numerator={asNumber(row.datasetHvdDataServiceNameCount)}
                                            denominator={asNumber(row.datasetHvdDataServiceNameTotal)}
                                        />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnený prístupový bod dátovej služby</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator
                                            numerator={asNumber(row.datasetHvdDataServiceEndpointCount)}
                                            denominator={asNumber(row.datasetHvdDataServiceEndpointTotal)}
                                        />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnený odkaz na dokumentáciu dátovej služby</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator
                                            numerator={asNumber(row.datasetHvdDataServiceDocumentationCount)}
                                            denominator={asNumber(row.datasetHvdDataServiceDocumentationTotal)}
                                        />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnený kontaktný bod dátovej služby, e-mail</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator
                                            numerator={asNumber(row.datasetHvdDataServiceContactCount)}
                                            denominator={asNumber(row.datasetHvdDataServiceContactTotal)}
                                        />
                                    </div>
                                </div>
                            ]}
                        />
                        <PageSubheader>Vyplnenie povinných metadát - dátové služby mimo HVD</PageSubheader>
                        <GridArrangement
                            columns={[
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnený názov dátovej služby</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator
                                            numerator={asNumber(row.datasetNonHvdDataServiceNameCount)}
                                            denominator={asNumber(row.datasetNonHvdDataServiceNameTotal)}
                                        />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnený prístupový bod dátovej služby</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator
                                            numerator={asNumber(row.datasetNonHvdDataServiceEndpointCount)}
                                            denominator={asNumber(row.datasetNonHvdDataServiceEndpointTotal)}
                                        />
                                    </div>
                                </div>
                            ]}
                        />
                        <PageSubheader>Vyplnenie odporúčaných metadát</PageSubheader>
                        <GridArrangement
                            columns={[
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnené časové pokrytie datasetu</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={asNumber(row.datasetTemporalCount)} denominator={asNumber(row.datasetTemporalTotal)} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnený kontaktný bod datasetu, e-mail</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={asNumber(row.datasetContactCount)} denominator={asNumber(row.datasetContactTotal)} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnený dátum vytvorenia datasetu</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator numerator={asNumber(row.datasetIssuedCount)} denominator={asNumber(row.datasetIssuedTotal)} />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnený kontaktný bod dátovej služby, e-mail</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator
                                            numerator={asNumber(row.datasetNonHvdDataServiceContactCount)}
                                            denominator={asNumber(row.datasetNonHvdDataServiceContactTotal)}
                                        />
                                    </div>
                                </div>,
                                <div className="nkod-detail-attribute">
                                    <div className="govuk-body nkod-detail-attribute-name">Vyplnený odkaz na dokumentáciu dátovej služby</div>
                                    <div className="govuk-body nkod-detail-attribute-value" data-testid="themes" style={{ wordBreak: 'break-word' }}>
                                        <Indicator
                                            numerator={asNumber(row.datasetNonHvdDataServiceDocumentationCount)}
                                            denominator={asNumber(row.datasetNonHvdDataServiceDocumentationTotal)}
                                        />
                                    </div>
                                </div>
                            ]}
                        />
                        <PageSubheader>Dostupnosť datasetov</PageSubheader>
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableHeaderCell style={{ overflowWrap: 'normal' }}>Atribút dostupnosti</TableHeaderCell>
                                    <TableHeaderCell style={{ overflowWrap: 'normal' }}>Dostupné</TableHeaderCell>
                                    <TableHeaderCell style={{ overflowWrap: 'normal' }}>Nedostupné</TableHeaderCell>
                                    <TableHeaderCell style={{ overflowWrap: 'normal' }}>Dostupnosť</TableHeaderCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                <AvailabilityTableRow
                                    title="Odkazy na stiahnutie"
                                    count={asNumber(row.availableDownloadLinkCount)}
                                    total={asNumber(row.availableDownloadLinkTotal)}
                                />
                                <AvailabilityTableRow
                                    title="Odkazy na schémy distribúcií"
                                    count={asNumber(row.availableSchemaLinkCount)}
                                    total={asNumber(row.availableSchemaLinkTotal)}
                                />
                                <AvailabilityTableRow
                                    title="Odkazy na dokumentácie datasetov"
                                    count={asNumber(row.availableDocumentationLinkCount)}
                                    total={asNumber(row.availableDocumentationLinkTotal)}
                                />
                                <AvailabilityTableRow
                                    title="Prístupové body dátových služieb"
                                    count={asNumber(row.availableDataServiceEndpointCount)}
                                    total={asNumber(row.availableDataServiceEndpointTotal)}
                                />
                                <AvailabilityTableRow
                                    title="Popis prístupových bodov dátových služieb"
                                    count={asNumber(row.availableDataServiceDescriptionCount)}
                                    total={asNumber(row.availableDataServiceDescriptionTotal)}
                                />
                                <AvailabilityTableRow
                                    title="CORS odkazy na stiahnutie"
                                    count={asNumber(row.corsEnabledDownloadLinkCount)}
                                    total={asNumber(row.corsEnabledDownloadLinkTotal)}
                                />
                                <AvailabilityTableRow
                                    title="CORS odkazy na schémy distribúcií"
                                    count={asNumber(row.corsEnabledSchemaLinkCount)}
                                    total={asNumber(row.corsEnabledSchemaLinkTotal)}
                                />
                                <AvailabilityTableRow
                                    title="CORS prístupové body dátových služieb"
                                    count={asNumber(row.corsEnabledDataServiceEndpointCount)}
                                    total={asNumber(row.corsEnabledDataServiceEndpointTotal)}
                                />
                                <AvailabilityTableRow
                                    title="CORS popis prístupových bodov dátových služieb"
                                    count={asNumber(row.corsEnabledDataServiceDescriptionCount)}
                                    total={asNumber(row.corsEnabledDataServiceDescriptionTotal)}
                                />
                            </TableBody>
                        </Table>

                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableHeaderCell style={{ overflowWrap: 'normal' }}>Atribút zhody</TableHeaderCell>
                                    <TableHeaderCell style={{ overflowWrap: 'normal' }}>Zhoda</TableHeaderCell>
                                    <TableHeaderCell style={{ overflowWrap: 'normal' }}>Nezhoda</TableHeaderCell>
                                    <TableHeaderCell style={{ overflowWrap: 'normal' }}>Podiel zhody</TableHeaderCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                <AvailabilityTableRow
                                    title="Zhoda deklarovaného formátu distribúcií so zisteným"
                                    count={asNumber(row.matchContentTypeCount)}
                                    total={asNumber(row.matchContentTypeTotal)}
                                />
                            </TableBody>
                        </Table>

                        <PageSubheader>Kvalita datasetov</PageSubheader>
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableHeaderCell style={{ overflowWrap: 'normal' }}>1*</TableHeaderCell>
                                    <TableHeaderCell style={{ overflowWrap: 'normal' }}>2*</TableHeaderCell>
                                    <TableHeaderCell style={{ overflowWrap: 'normal' }}>3*</TableHeaderCell>
                                    <TableHeaderCell style={{ overflowWrap: 'normal' }}>4*</TableHeaderCell>
                                    <TableHeaderCell style={{ overflowWrap: 'normal' }}>5*</TableHeaderCell>
                                    <TableHeaderCell style={{ overflowWrap: 'normal' }}>3* a viac</TableHeaderCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                <TableRow>
                                    <TableCell>{datasetQualityCount[0]}</TableCell>
                                    <TableCell>{datasetQualityCount[1]}</TableCell>
                                    <TableCell>{datasetQualityCount[2]}</TableCell>
                                    <TableCell>{datasetQualityCount[3]}</TableCell>
                                    <TableCell>{datasetQualityCount[4]}</TableCell>
                                    <TableCell>
                                        <div style={{ display: 'flex', alignItems: 'center' }}>
                                            <span style={{ marginRight: '20px', whiteSpace: 'nowrap' }}>{datasetQualitySum}</span>
                                            <Indicator numerator={datasetQualitySum} denominator={datasetQualitySumAll} />
                                        </div>
                                    </TableCell>
                                </TableRow>
                            </TableBody>
                        </Table>
                        <PageSubheader>Otvorenosť licencií - otvorené licencie</PageSubheader>
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableHeaderCell style={{ overflowWrap: 'normal' }}>Otvorené licencie</TableHeaderCell>
                                    <TableHeaderCell style={{ overflowWrap: 'normal' }}>Iné licencie</TableHeaderCell>
                                    <TableHeaderCell style={{ overflowWrap: 'normal' }}>Otvorenosť licencií</TableHeaderCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                <TableRow>
                                    <TableCell>{asNumber(row.datasetLicenseOpenCount)}</TableCell>
                                    <TableCell>{Math.max(asNumber(row.datasetLicenseTotalCount) - asNumber(row.datasetLicenseOpenCount), 0)}</TableCell>
                                    <TableCell>
                                        <Indicator numerator={asNumber(row.datasetLicenseOpenCount)} denominator={asNumber(row.datasetLicenseTotalCount)} />
                                    </TableCell>
                                </TableRow>
                            </TableBody>
                        </Table>

                        <PublisherDatasets publisherId={id} />
                    </>
                ) : null}
            </MainContent>
        </>
    );
}

function AvailabilityTableRow({ title, count, total }: { title: string; count?: number | null | undefined; total?: number | null | undefined }) {
    const useTotal = Math.max(total ?? 0, 0);
    const useCount = Math.min(Math.max(count ?? 0, 0), useTotal);

    return (
        <TableRow>
            <TableCell>{title}</TableCell>
            <TableCell>{useCount}</TableCell>
            <TableCell>{useTotal - useCount}</TableCell>
            <TableCell>
                <Indicator numerator={useCount} denominator={useTotal} />
            </TableCell>
        </TableRow>
    );
}
