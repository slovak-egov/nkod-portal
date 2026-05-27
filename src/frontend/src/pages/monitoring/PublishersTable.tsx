import SparqlEnabledTable from './SparqlEnabledTable';
import { formatNumber, SparqlQuery } from './tools';
import { Link } from 'react-router-dom';
import { HarvestationResult, LiteralColumn, UriColumn } from './types';

type Row = {
    id: UriColumn;
    name: LiteralColumn;
    publisher: LiteralColumn;
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
    select: ['?id', '?publisher', '?datasetsCount', '?newDatasetsCount', '?modifiedDatasetsCount', '?removedDatasetsCount'],
    where: [
        `?parent a m:HarvestationResult ;
                   m:hasSubResult ?id .

        ?id a m:HarvestationResult ;
              m:context ?p ;
              m:datasetsCount ?datasetsCount ;
              m:newDatasetsCount ?newDatasetsCount ;
              m:modifiedDatasetsCount ?modifiedDatasetsCount ;
              m:removedDatasetsCount ?removedDatasetsCount .
            
        ?p a foaf:Agent;
             foaf:name ?publisher .
             
        FILTER(LANG(?publisher) = "sk")
        FILTER(?datasetsCount > 0 || ?newDatasetsCount > 0 || ?modifiedDatasetsCount > 0 || ?removedDatasetsCount > 0)`
    ],
    defaultOrderBy: [{ propertyName: '?datasetsCount', ascending: false }]
};

export default function PublishersTable(props: Props) {
    const { parent } = props;

    const query = { ...genericQuery, where: [...(genericQuery.where ?? [])] };
    query.where.push(`FILTER(?parent = <${parent}>)`);

    return (
        <SparqlEnabledTable<Row>
            query={query}
            header="Poskytovatelia dát %count%"
            pageSize={10}
            columns={[
                {
                    header: 'Poskytovateľ',
                    value: (row) => (
                        <Link to={'/harvestacie/poskytovatel/detail/' + encodeURIComponent(row.id.value)} className="idsk-card-title govuk-link">
                            {row.publisher?.value}
                        </Link>
                    ),
                    sortProperty: '?publisher'
                },
                {
                    header: 'Počet datasetov',
                    value: (row) => formatNumber(row.datasetsCount?.value),
                    sortProperty: '?datasetsCount'
                },
                {
                    header: 'Nové / upravené / odstránené datasety',
                    value: (row) => {
                        return (
                            <>
                                <div style={{ textAlign: 'center' }}>
                                    {formatNumber(row.newDatasetsCount?.value)} / {formatNumber(row.modifiedDatasetsCount?.value)} /{' '}
                                    {formatNumber(row.removedDatasetsCount?.value)}
                                </div>
                            </>
                        );
                    }
                }
            ]}
        />
    );
}
