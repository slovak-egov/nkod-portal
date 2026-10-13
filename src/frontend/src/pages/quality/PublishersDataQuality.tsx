import Breadcrumbs from '../../components/Breadcrumbs';
import MainContent from '../../components/MainContent';
import { t } from 'i18next';
import PageHeader from '../../components/PageHeader';
import Table from '../../components/Table';
import TableHead from '../../components/TableHead';
import TableRow from '../../components/TableRow';
import TableHeaderCell from '../../components/TableHeaderCell';
import TableBody from '../../components/TableBody';
import TableCell from '../../components/TableCell';
import Indicator from './Indicator';
import { knownCodelists, useCodelists } from '../../client';
import TableFilters from '../../components/TableFilters';
import { useState } from 'react';
import SparqlEnabledTable from '../monitoring/SparqlEnabledTable';
import { escapeSparqlText, SparqlQuery } from '../monitoring/tools';
import { LiteralColumn, UriColumn } from '../monitoring/types';
import { Link } from 'react-router-dom';

const requiredCodelists = [knownCodelists.publisher.legalForm];

type Publisher = {
    id?: UriColumn | null | undefined;
    name?: LiteralColumn | null | undefined;

    datasetsCount?: LiteralColumn | null | undefined;
    dataSeriesCount?: LiteralColumn | null | undefined;
    datasetsPublicationMinimumCount?: LiteralColumn | null | undefined;

    requiredMetadataCount?: LiteralColumn | null | undefined;
    requiredMetadataTotal?: LiteralColumn | null | undefined;
    optionalMetadataCount?: LiteralColumn | null | undefined;
    optionalMetadataTotal?: LiteralColumn | null | undefined;

    datasetQuality1Count?: LiteralColumn | null | undefined;
    datasetQuality2Count?: LiteralColumn | null | undefined;
    datasetQuality3Count?: LiteralColumn | null | undefined;
    datasetQuality4Count?: LiteralColumn | null | undefined;
    datasetQuality5Count?: LiteralColumn | null | undefined;

    datasetLicenseOpenCount?: LiteralColumn | null | undefined;

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
};

const defaultQuery: SparqlQuery = {
    prefixes: [
        'foaf: <http://xmlns.com/foaf/0.1/>',
        'dqv: <http://www.w3.org/ns/dqv#>',
        ': <https://data.gov.sk/def/observation/data-quality/metrics/>',
        'xsd: <http://www.w3.org/2001/XMLSchema#>'
    ],
    select: ['?id', '?name'],
    where: [
        `?id a foaf:Agent ;
        foaf:name ?name .
    
        FILTER(langMatches(LANG(?name), "sk"))`
    ],
    defaultOrderBy: [{ propertyName: '?name', ascending: true }]
};

function addMeasurement(query: SparqlQuery, variable: string, measurement: string) {
    query.select.push(`?${variable}`);
    query.where ??= [];
    query.where.push(`OPTIONAL { ?id dqv:hasQualityMeasurement [ dqv:isMeasurementOf :${measurement} ; dqv:value ?${variable} ] . }`);
}

addMeasurement(defaultQuery, 'requiredMetadataCount', 'pocetSplnenychKontrolNaVyplneniePovinnychMetadat');
addMeasurement(defaultQuery, 'requiredMetadataTotal', 'pocetKontrolNaVyplneniePovinnychMetadat');
addMeasurement(defaultQuery, 'optionalMetadataCount', 'pocetSplnenychKontrolNaVyplnenieOdporucanychMetadat');
addMeasurement(defaultQuery, 'optionalMetadataTotal', 'pocetKontrolNaVyplnenieOdporucanychMetadat');

addMeasurement(defaultQuery, 'datasetsCount', 'pocetDatasetovPoskytovatela');
addMeasurement(defaultQuery, 'dataSeriesCount', 'pocetDatovychSeriiPoskytovatela');
addMeasurement(defaultQuery, 'datasetsPublicationMinimumCount', 'pocetDatasetovPublikacnehoMinimaPoskytovatela');
addMeasurement(defaultQuery, 'datasetQuality1Count', 'pocetDatasetovSKvalitou1');
addMeasurement(defaultQuery, 'datasetQuality2Count', 'pocetDatasetovSKvalitou2');
addMeasurement(defaultQuery, 'datasetQuality3Count', 'pocetDatasetovSKvalitou3');
addMeasurement(defaultQuery, 'datasetQuality4Count', 'pocetDatasetovSKvalitou4');
addMeasurement(defaultQuery, 'datasetQuality5Count', 'pocetDatasetovSKvalitou5');
addMeasurement(defaultQuery, 'datasetLicenseOpenCount', 'pocetDatasetovSOtvorenouLicenciou');

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

const availabilityProperties = [
    'availableDownloadLink',
    'availableSchemaLink',
    'availableDocumentationLink',
    'availableDataServiceEndpoint',
    'availableDataServiceDescription',
    'corsEnabledDownloadLink',
    'corsEnabledSchemaLink',
    'corsEnabledDataServiceEndpoint',
    'corsEnabledDataServiceDescription'
];

function sparqlNumber(variable: string): string {
    return `COALESCE(xsd:decimal(?${variable}), 0)`;
}

function sparqlSum(variables: string[]): string {
    return `(${variables.map(sparqlNumber).join(' + ')})`;
}

function sparqlShare(numerator: string, denominator: string): string {
    return `(IF(${denominator} > 0, ${numerator} / ${denominator}, -1))`;
}

const pureDatasetCountExpression = `(${sparqlNumber('datasetsCount')} - ${sparqlNumber('dataSeriesCount')})`;

const sortExpressions = {
    requiredMetadata: sparqlShare(sparqlNumber('requiredMetadataCount'), sparqlNumber('requiredMetadataTotal')),
    optionalMetadata: sparqlShare(sparqlNumber('optionalMetadataCount'), sparqlNumber('optionalMetadataTotal')),
    availability: sparqlShare(sparqlSum(availabilityProperties.map((p) => p + 'Count')), sparqlSum(availabilityProperties.map((p) => p + 'Total'))),
    datasetQuality: sparqlShare(sparqlSum(['datasetQuality3Count', 'datasetQuality4Count', 'datasetQuality5Count']), pureDatasetCountExpression),
    licenseOpenness: sparqlShare(sparqlNumber('datasetLicenseOpenCount'), pureDatasetCountExpression),
    publicationMinimum: `(${sparqlNumber('datasetsPublicationMinimumCount')})`
};

function SparqlIndicator<T extends Record<string, any>>({ source, property }: { property: string; source: T }) {
    const numeratorText = (source[property + 'Count'] as LiteralColumn)?.value;
    const denominatorText = (source[property + 'Total'] as LiteralColumn)?.value;

    const numerator = parseInt(numeratorText ?? '0', 10);
    const denominator = parseInt(denominatorText ?? '0', 10);
    return <Indicator numerator={numerator} denominator={denominator} />;
}

function asNumber(column?: LiteralColumn | null | undefined): number {
    if (column !== null && column !== undefined) {
        const n = Number(column.value);
        if (!isNaN(n)) {
            return n;
        }
    }
    return 0;
}

export default function PublishersDataQuality() {
    const [searchQuery, setSearchQuery] = useState('');
    const [selectedLegalForm, setSelectedLegalForm] = useState('');
    const [codelists] = useCodelists(requiredCodelists);
    const legalFormCodelist = codelists.find((c) => c.id === knownCodelists.publisher.legalForm);

    const legalFormOptions = [{ value: '', label: 'všetky' }, ...(legalFormCodelist?.values.map((v) => ({ value: v.id, label: v.label })) ?? [])];

    const query = { ...defaultQuery, where: [...(defaultQuery.where ?? [])] };

    if (selectedLegalForm) {
        query.where ??= [];
        query.where.push(`?id <https://data.gov.sk/def/ontology/legal-subject/legalFormType> <${selectedLegalForm}>`);
    }

    const queryParts = searchQuery.split(' ').filter((part) => part.trim() !== '');
    if (queryParts.length > 0) {
        query.where ??= [];
        queryParts.forEach((p) => {
            const escaped = escapeSparqlText(p);
            query.where.push(`FILTER(CONTAINS(LCASE(?name), LCASE("${escaped}")))`);
        });
    }

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: 'Kvalita metadát' }]} />
            <MainContent>
                <PageHeader>Kvalita metadát a datasetov</PageHeader>
                {legalFormCodelist ? (
                    <>
                        <SparqlEnabledTable<Publisher>
                            alwaysShowTable
                            query={query}
                            filter={
                                <TableFilters
                                    filters={[
                                        {
                                            id: 'search',
                                            label: t('search'),
                                            type: 'text'
                                        },
                                        {
                                            id: 'legalForm',
                                            label: t('legalForm'),
                                            type: 'select',
                                            options: legalFormOptions
                                        }
                                    ]}
                                    filterValues={[
                                        { id: 'search', value: searchQuery },
                                        { id: 'legalForm', value: selectedLegalForm }
                                    ]}
                                    onFilter={(filters) => {
                                        setSelectedLegalForm(filters.find((f) => f.id === 'legalForm')?.value ?? '');
                                        setSearchQuery(filters.find((f) => f.id === 'search')?.value ?? '');
                                    }}
                                />
                            }
                            columns={[
                                {
                                    header: 'Názov poskytovateľa',
                                    headerStyle: { width: '100px' },
                                    cellStyle: { width: '100px' },
                                    value: (r) => (
                                        <Link to={'/kvalita-metadat-new/' + encodeURIComponent(r.id?.value ?? '')} className="idsk-card-title govuk-link">
                                            {r?.name?.value ?? ''}
                                        </Link>
                                    ),
                                    sortProperty: '?name'
                                },
                                {
                                    header: 'Vyplnenie povinných metadát',
                                    value: (r) => <SparqlIndicator property="requiredMetadata" source={r} />,
                                    sortProperty: sortExpressions.requiredMetadata
                                },
                                {
                                    header: 'Vyplnenie odporúčaných metadát',
                                    value: (r) => <SparqlIndicator property="optionalMetadata" source={r} />,
                                    sortProperty: sortExpressions.optionalMetadata
                                },
                                {
                                    header: 'Dostupnosť datasetov',
                                    sortProperty: sortExpressions.availability,
                                    value: (r) => {
                                        const availabilityCount =
                                            asNumber(r?.availableDownloadLinkCount) +
                                            asNumber(r?.availableSchemaLinkCount) +
                                            asNumber(r?.availableDocumentationLinkCount) +
                                            asNumber(r?.availableDataServiceEndpointCount) +
                                            asNumber(r?.availableDataServiceDescriptionCount) +
                                            asNumber(r?.corsEnabledDownloadLinkCount) +
                                            asNumber(r?.corsEnabledSchemaLinkCount) +
                                            asNumber(r?.corsEnabledDataServiceEndpointCount) +
                                            asNumber(r?.corsEnabledDataServiceDescriptionCount);

                                        const availabilityTotal =
                                            asNumber(r?.availableDownloadLinkTotal) +
                                            asNumber(r?.availableSchemaLinkTotal) +
                                            asNumber(r?.availableDocumentationLinkTotal) +
                                            asNumber(r?.availableDataServiceEndpointTotal) +
                                            asNumber(r?.availableDataServiceDescriptionTotal) +
                                            asNumber(r?.corsEnabledDownloadLinkTotal) +
                                            asNumber(r?.corsEnabledSchemaLinkTotal) +
                                            asNumber(r?.corsEnabledDataServiceEndpointTotal) +
                                            asNumber(r?.corsEnabledDataServiceDescriptionTotal);

                                        return <Indicator numerator={availabilityCount} denominator={availabilityTotal} />;
                                    }
                                },
                                {
                                    header: 'Kvalita datasetov',
                                    sortProperty: sortExpressions.datasetQuality,
                                    value: (r) => {
                                        const useDatasetCount = Math.max(asNumber(r?.datasetsCount), 0);
                                        const useDataseriesCount = Math.max(asNumber(r?.dataSeriesCount), 0);
                                        const pureDatasetCount = Math.max(useDatasetCount - useDataseriesCount, 0);

                                        const datasetQualityCount = [];
                                        for (let i = 1; i <= 5; i++) {
                                            datasetQualityCount.push(
                                                Math.max(asNumber(r?.[`datasetQuality${i}Count` as keyof Publisher] as LiteralColumn | null | undefined), 0)
                                            );
                                        }

                                        const datasetQualitySum = datasetQualityCount[2] + datasetQualityCount[3] + datasetQualityCount[4];
                                        return <Indicator numerator={datasetQualitySum} denominator={pureDatasetCount} />;
                                    }
                                },
                                {
                                    header: 'Otvorenosť licencií',
                                    sortProperty: sortExpressions.licenseOpenness,
                                    value: (r) => {
                                        const useDatasetCount = Math.max(asNumber(r?.datasetsCount), 0);
                                        const useDataseriesCount = Math.max(asNumber(r?.dataSeriesCount), 0);
                                        const pureDatasetCount = Math.max(useDatasetCount - useDataseriesCount, 0);

                                        return <Indicator numerator={asNumber(r?.datasetLicenseOpenCount)} denominator={pureDatasetCount} />;
                                    }
                                },
                                {
                                    header: 'Počet datasetov Publikačného minima',
                                    value: (r) => asNumber(r?.datasetsPublicationMinimumCount),
                                    sortProperty: sortExpressions.publicationMinimum
                                }
                            ]}
                        />
                    </>
                ) : null}
            </MainContent>
        </>
    );
}
