import SparqlEnabledTable from './SparqlEnabledTable';
import { SparqlQuery } from './tools';
import { HarvestationResult, LiteralColumn, UriColumn } from './types';

type Row = {
    id: UriColumn;
    note: LiteralColumn;
    name: LiteralColumn;
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
    select: ['?name', '?note'],
    where: [
        `?parent a m:HarvestationResult ;
                   m:hasProblem ?id .

         ?id a m:HarvestationProblem ;
              m:name ?name ;
              m:note ?note .
              
        FILTER NOT EXISTS { ?id a m:HarvestationSource }`
    ],
    defaultOrderBy: [{ propertyName: '?name', ascending: true }]
};

export default function IdentifiedProblems(props: Props) {
    const { parent } = props;

    const query = { ...genericQuery, where: [...(genericQuery.where ?? [])] };
    query.where.push(`FILTER(?parent = <${parent}>)`);

    return (
        <SparqlEnabledTable<Row>
            query={query}
            header="Identifikované problémy %count%"
            pageSize={10}
            columns={[
                {
                    header: 'Názov',
                    value: (row) => <div style={{ whiteSpace: 'pre-wrap', fontSize: '0.8em' }}>{row.name?.value}</div>,
                    sortProperty: '?name'
                },
                {
                    header: 'Poznámka',
                    value: (row) => <div style={{ whiteSpace: 'pre-wrap', fontSize: '0.8em' }}>{row.note?.value}</div>,
                    sortProperty: '?note'
                }
            ]}
            exportDefintion={{
                columns: [
                    { header: 'Názov', value: (row) => row.name?.value },
                    { header: 'Poznámka', value: (row) => row.note?.value }
                ]
            }}
        />
    );
}
