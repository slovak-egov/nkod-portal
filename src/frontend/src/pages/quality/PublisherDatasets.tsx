import { t } from 'i18next';
import PageSubheader from '../../components/PageSubHeader';
import { escapeSparqlText, SparqlQuery } from '../monitoring/tools';
import { useState } from 'react';
import { LiteralColumn, UriColumn } from '../monitoring/types';
import SparqlEnabledTable from '../monitoring/SparqlEnabledTable';
import TableFilters from '../../components/TableFilters';

type Props = {
    publisherId: string;
};

type Row = {
    dataset?: UriColumn | null | undefined;
    name?: LiteralColumn | null | undefined;
    requiredMetadata?: LiteralColumn | null | undefined;
    recommendedMetadata?: LiteralColumn | null | undefined;
    datasetQuality3?: LiteralColumn | null | undefined;
    datasetQuality4?: LiteralColumn | null | undefined;
    datasetQuality5?: LiteralColumn | null | undefined;
    openLicense?: LiteralColumn | null | undefined;
    availablity?: LiteralColumn | null | undefined;
};

const defaultQuery: SparqlQuery = {
    prefixes: [
        'foaf: <http://xmlns.com/foaf/0.1/>',
        'dqv: <http://www.w3.org/ns/dqv#>',
        ': <https://data.gov.sk/def/observation/data-quality/metrics/>',
        'dcat: <http://www.w3.org/ns/dcat#>',
        'dct: <http://purl.org/dc/terms/>'
    ],
    select: ['?dataset', '?name'],
    where: [
        `{
          ?dataset a dcat:Dataset .
         }
         UNION
         {
           ?dataset a dcat:DatasetSeries .
         }
         ?dataset dct:publisher ?publisher ;
                  dct:title ?name .

        FILTER(LANG(?name) = "sk")`
    ]
};

function addMeasurement(query: SparqlQuery, variable: string, measurement: string) {
    query.select.push(`?${variable}`);
    query.where ??= [];
    query.where.push(`OPTIONAL { ?dataset dqv:hasQualityMeasurement [ dqv:isMeasurementOf :${measurement} ; dqv:value ?${variable} ] . }`);
}

addMeasurement(defaultQuery, 'requiredMetadata', 'vyplneniePovinnychMetadat');
addMeasurement(defaultQuery, 'recommendedMetadata', 'vyplnenieOdporucanychMetadat');

addMeasurement(defaultQuery, 'datasetQuality3', 'kvalitaDatasetu3');
addMeasurement(defaultQuery, 'datasetQuality4', 'kvalitaDatasetu4');
addMeasurement(defaultQuery, 'datasetQuality5', 'kvalitaDatasetu5');
addMeasurement(defaultQuery, 'openLicense', 'sOtvorenouLicenciou');

addMeasurement(defaultQuery, 'availablity', 'metrikaDostupnostiDatasetu');

const conditions = [
    '!BOUND(?requiredMetadata) || !?requiredMetadata',
    '!BOUND(?recommendedMetadata) || !?recommendedMetadata',
    '!((BOUND(?datasetQuality3) && ?datasetQuality3) || (BOUND(?datasetQuality4) && ?datasetQuality4) || (BOUND(?datasetQuality5) && ?datasetQuality5))',
    '!BOUND(?openLicense) || !?openLicense',
    '!BOUND(?availablity) || !?availablity'
];

defaultQuery.where ??= [];
defaultQuery.where.push(`FILTER(${conditions.map((c) => `(${c})`).join(' || ')})`);

function sparqlBoolean(variable: string): string {
    return `COALESCE(STR(?${variable}) = "true", false)`;
}

// Vyraz pre zoradenie; splnena podmienka = 1, nesplnena (zobrazene ako varovanie) = 0
function sparqlSortFlag(condition: string): string {
    return `(IF(${condition}, 1, 0))`;
}

const sortExpressions = {
    requiredMetadata: sparqlSortFlag(sparqlBoolean('requiredMetadata')),
    recommendedMetadata: sparqlSortFlag(sparqlBoolean('recommendedMetadata')),
    availablity: sparqlSortFlag(sparqlBoolean('availablity')),
    datasetQuality: sparqlSortFlag(['datasetQuality3', 'datasetQuality4', 'datasetQuality5'].map(sparqlBoolean).join(' || ')),
    openLicense: sparqlSortFlag(sparqlBoolean('openLicense'))
};

function asBoolean(column?: LiteralColumn | null | undefined): boolean {
    if (column !== null && column !== undefined) {
        return column.value === 'true';
    }
    return false;
}

export default function PublisherDatasets({ publisherId }: Props) {
    const [searchQuery, setSearchQuery] = useState('');
    const query = { ...defaultQuery, where: [...(defaultQuery.where ?? [])] };
    query.where.push(`FILTER(?publisher = <${publisherId}>)`);

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
            <PageSubheader>Problematické datasety</PageSubheader>
            <SparqlEnabledTable<Row>
                query={query}
                alwaysShowTable
                filter={
                    <TableFilters
                        filters={[
                            {
                                id: 'search',
                                label: t('search'),
                                type: 'text'
                            }
                        ]}
                        filterValues={[{ id: 'search', value: searchQuery }]}
                        onFilter={(filters) => {
                            setSearchQuery(filters.find((f) => f.id === 'search')?.value ?? '');
                        }}
                    />
                }
                columns={[
                    {
                        header: 'Názov datasetu',
                        value: (row) => <span>{row.name?.value}</span>,
                        sortProperty: '?name'
                    },
                    {
                        header: 'Vyplnenie povinných metadát',
                        sortProperty: sortExpressions.requiredMetadata,
                        value: (row) => (
                            <TableCellWarning isError={!asBoolean(row.requiredMetadata)}>dataset nespĺňa požiadavky pre povinné metadáta</TableCellWarning>
                        )
                    },
                    {
                        header: 'Vyplnenie odporúčaných metadát',
                        sortProperty: sortExpressions.recommendedMetadata,
                        value: (row) => (
                            <TableCellWarning isError={!asBoolean(row.recommendedMetadata)}>
                                dataset nespĺňa odporúčania pre nepovinné metadáta
                            </TableCellWarning>
                        )
                    },
                    {
                        header: 'Dostupnosť datasetu',
                        sortProperty: sortExpressions.availablity,
                        value: (row) => <TableCellWarning isError={!asBoolean(row.availablity)}>dataset nie je plne dostupný</TableCellWarning>
                    },
                    {
                        header: 'Kvalita datasetu',
                        sortProperty: sortExpressions.datasetQuality,
                        value: (row) => (
                            <TableCellWarning isError={!(asBoolean(row.datasetQuality3) || asBoolean(row.datasetQuality4) || asBoolean(row.datasetQuality5))}>
                                dataset nespĺňa požiadavky pre kvalitu datasetu
                            </TableCellWarning>
                        )
                    },
                    {
                        header: 'Otvorenosť licencie',
                        sortProperty: sortExpressions.openLicense,
                        value: (row) => <TableCellWarning isError={!asBoolean(row.openLicense)}>dataset nemá otvorenú licenciu</TableCellWarning>
                    }
                ]}
            />
        </>
    );
}

function TableCellWarning({ isError, children }: { isError: boolean; children: React.ReactNode }) {
    return isError ? (
        <div style={{ color: 'red', textAlign: 'center' }}>
            ⚠ <span className="govuk-visually-hidden">{children}</span>
        </div>
    ) : null;
}
