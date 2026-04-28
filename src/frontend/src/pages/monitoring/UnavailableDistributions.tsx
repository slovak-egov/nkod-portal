import SparqlEnabledTable from './SparqlEnabledTable';
import { SparqlQuery } from './tools';
import { HarvestationResult, LiteralColumn, UriColumn } from './types';

type Row = {
    id: UriColumn;
    note: LiteralColumn;
    download: LiteralColumn;
    publisher: UriColumn;
    dataset: UriColumn;
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
    select: ['?publisher', '?dataset', '?download', '?note'],
    where: [
        `?parent a m:HarvestationResult ;
                   m:hasDistributionSource ?id .

        ?id a m:HarvestationSource ;
              m:success false ;
              m:name ?download ;
              m:note ?note .
        
        OPTIONAL {
            ?id a m:HarvestationProblem ;
                  m:publisher ?p .

            ?p a foaf:Agent ;
                 foaf:name ?publisher .

            FILTER(LANG(?publisher) = "sk")
        }
             
        OPTIONAL {
            ?id a m:HarvestationProblem ;
                  m:dataset ?d .

            ?d a dcat:Dataset ;
                 dct:title ?dataset .

            FILTER(LANG(?dataset) = "sk")
        }`
    ],
    defaultOrderBy: [
        { propertyName: '?publisher', ascending: true },
        { propertyName: '?dataset', ascending: true }
    ]
};

export default function UnavailableDistributions(props: Props) {
    const { parent } = props;

    const query = { ...genericQuery, where: [...(genericQuery.where ?? [])] };
    query.where.push(`FILTER(?parent = <${parent}>)`);

    return (
        <SparqlEnabledTable<Row>
            query={query}
            header="Nedostupné súbory %count%"
            pageSize={10}
            columns={[
                {
                    header: 'Poskytovateľ',
                    value: (row) => row.publisher?.value,
                    sortProperty: '?publisher'
                },
                {
                    header: 'Dataset',
                    value: (row) => row.dataset?.value,
                    sortProperty: '?dataset'
                },
                {
                    header: 'Súbor',
                    value: (row) => row.download?.value,
                    sortProperty: '?download'
                },
                {
                    header: 'Poznámka',
                    value: (row) => row.note?.value,
                    sortProperty: '?note'
                }
            ]}
            exportDefintion={{
                columns: [
                    { header: 'Poskytovateľ', value: (row) => row.publisher?.value },
                    { header: 'Dataset', value: (row) => row.dataset?.value },
                    { header: 'Súbor', value: (row) => row.download?.value },
                    { header: 'Poznámka', value: (row) => row.note?.value }
                ]
            }}
        />
    );
}
