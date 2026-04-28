import SparqlEnabledTable from './SparqlEnabledTable';
import { formatNumber, getNumber, SparqlQuery } from './tools';
import { Link } from 'react-router-dom';
import { DurationColumn, HarvestationResult, IntColumn, LiteralColumn, UriColumn } from './types';
import ExternalLink from './ExternalLink';

type Row = {
    id: UriColumn;
    name: LiteralColumn;
    publisher: LiteralColumn;
    endpoint: LiteralColumn;
    duration: DurationColumn;
    averageDuration: DurationColumn;
    invalidSourcesCount: IntColumn;
} & HarvestationResult;

type Props = {
    parent: string | null | undefined;
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
        '?problemsCount'
    ],
    where: [
        `?parent a m:HarvestationResult ;
                   m:hasSubResult ?id .

        ?id a m:HarvestationResult ;
            m:context ?c ;
            m:datasetsCount ?datasetsCount ;
            m:newDatasetsCount ?newDatasetsCount ;
            m:modifiedDatasetsCount ?modifiedDatasetsCount ;
            m:removedDatasetsCount ?removedDatasetsCount ;
            m:externalSourcesDuration ?duration ;
            m:externalSourcesAverageDuration ?averageDuration ;
            m:problemsCount ?problemsCount ;
            m:invalidExternalSourcesCount ?invalidSourcesCount .
    
        ?c a dcat:Catalog ;
            dct:publisher ?p ;
            dct:title ?name ;
            dcat:endpointURL ?endpoint .
            
        ?p a foaf:Agent;
             foaf:name ?publisher .
             
        FILTER(LANG(?name) = "sk")
        FILTER(LANG(?publisher) = "sk")`
    ],
    defaultOrderBy: [
        { propertyName: '?invalidSourcesCount', ascending: false },
        { propertyName: '?datasetsCount', ascending: false }
    ]
};

export default function LocalCatalogsTable(props: Props) {
    const { parent } = props;

    const query = { ...genericQuery, where: [...(genericQuery.where ?? [])] };
    query.where.push(`FILTER(?parent = <${parent}>)`);

    return (
        <SparqlEnabledTable<Row>
            query={query}
            pageSize={10}
            header={'Lokálne katalógy %count%'}
            columns={[
                {
                    header: 'Názov',
                    value: (row) => (
                        <Link to={'/harvestacie/katalog/detail/' + encodeURIComponent(row.id.value)} className="idsk-card-title govuk-link">
                            {row.name?.value}
                        </Link>
                    ),
                    sortProperty: '?name'
                },
                {
                    header: 'Poskytovateľ',
                    value: (row) => row.publisher?.value,
                    sortProperty: '?publisher'
                },
                {
                    header: 'Prístupový bod',
                    value: (row) => <ExternalLink href={row.endpoint?.value} />
                },
                {
                    header: 'Datasety',
                    value: (row) => <div style={{ textAlign: 'center' }}>{formatNumber(row.datasetsCount?.value)}</div>,
                    sortProperty: '?datasetsCount'
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
        />
    );
}
