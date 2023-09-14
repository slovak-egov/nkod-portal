import PageHeader from "../components/PageHeader";
import Button from "../components/Button";
import Table from "../components/Table";
import TableHead from "../components/TableHead";
import TableRow from "../components/TableRow";
import TableHeaderCell from "../components/TableHeaderCell";
import TableBody from "../components/TableBody";
import TableCell from "../components/TableCell";
import Pagination from "../components/Pagination";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import { useDatasets } from "../client";
import ErrorAlert from "../components/ErrorAlert";
import { useNavigate } from "react-router";

export default function DatasetList()
{
    const [datasets, query, setQueryParameters, loading, error] = useDatasets({filters: {publisher: ['https://data.gov.sk/id/legal-subject/00166197']}});
    const navigate = useNavigate();

    return <>
        <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Zoznam datasetov'}]} />
        <MainContent>
            <PageHeader>Zoznam datasetov</PageHeader>

            <p className="govuk-body nkod-publisher-name">
                <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>Poskytovateľ dát</span><br />
                    Ministerstvo investícií, regionálneho rozvoja a informatizácie Slovenskej republiky 
                </p>
            <p>
                <Button onClick={() => navigate('/sprava/datasety/pridat')}>Nový dataset</Button>
            </p>

            {loading ? <div>Loading...</div> : error ? <ErrorAlert error={error} /> : datasets ? <>
                {datasets.items.length > 0 ? <><Table>
                    <TableHead>
                        <TableRow>
                            <TableHeaderCell>
                                Názov
                            </TableHeaderCell>
                            <TableHeaderCell>
                                Stav
                            </TableHeaderCell>
                            <TableHeaderCell>
                                Distribúcie
                            </TableHeaderCell>
                            <TableHeaderCell>
                                Nástroje
                            </TableHeaderCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {datasets.items.map(d => <TableRow key={d.id}>
                            <TableCell>
                                {d.name}
                            </TableCell>
                            <TableCell>
                                {d.isPublic ? 'publikovaný' : 'nepublikovaný'}
                            </TableCell>
                            <TableCell>
                                <div>
                                    {d.distributions.map(distribution => {
                                        if (distribution.downloadUrl && distribution.formatValue) {
                                            return <a href={distribution.downloadUrl} className="govuk-link" key={distribution.id} style={{marginRight: '0.5em'}}>
                                                {distribution.formatValue.label}
                                            </a>
                                        }
                                        return null;
                                    })}
                                </div>
                                <Button className="idsk-button idsk-button--secondary" style={{marginTop: '10px', marginBottom: '10px'}}>Zmeniť distribúcie</Button>
                            </TableCell>
                            <TableCell>
                                <Button className="idsk-button idsk-button--secondary" style={{marginRight: '10px', marginTop: '10px', marginBottom: '10px'}}>Upraviť</Button>
                                <Button className="idsk-button idsk-button--secondary" style={{marginTop: '10px', marginBottom: '10px'}}>Odstrániť</Button>
                            </TableCell>
                        </TableRow>)}
                    </TableBody>
                </Table>
                <Pagination totalItems={datasets.totalCount} pageSize={query.pageSize} currentPage={query.page} onPageChange={p => setQueryParameters({page: p})} /></> : <div>No datasets found</div>}
            </> : null}
        </MainContent>
        </>;
}