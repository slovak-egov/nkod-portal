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
import { removeDataset, useDatasets, useUserInfo } from "../client";
import ErrorAlert from "../components/ErrorAlert";
import { useNavigate } from "react-router";
import Loading from "../components/Loading";
import { useEffect } from "react";

export default function DatasetList()
{
    const [datasets, query, setQueryParameters, loading, error, refresh] = useDatasets({pageSize: 20, page: 0});
    const navigate = useNavigate();
    const [userInfo] = useUserInfo();

    useEffect(() => {
        if (userInfo?.publisher) {
            setQueryParameters({
                filters: {
                    publishers: [userInfo.publisher],
                },
                page: 1
            });
        }
    }, [userInfo]);

    return <>
        <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Zoznam datasetov'}]} />
        <MainContent>
            <PageHeader>Zoznam datasetov</PageHeader>

            {userInfo?.publisherView ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>Poskytovateľ dát</span><br />
                        {userInfo.publisherView.name}
                    </p> : null}
            <p>
                <Button onClick={() => navigate('/sprava/datasety/pridat')}>Nový dataset</Button>
            </p>

            {loading ? <Loading /> : error ? <ErrorAlert error={error} /> : datasets ? <>
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
                                <Button className="idsk-button idsk-button--secondary" style={{marginTop: '10px', marginBottom: '10px'}} onClick={() => navigate('/sprava/distribucie/' + d.id)}>Zmeniť distribúcie</Button>
                            </TableCell>
                            <TableCell>
                                <Button className="idsk-button idsk-button--secondary" style={{marginRight: '10px', marginTop: '10px', marginBottom: '10px'}} onClick={() => navigate('/sprava/datasety/upravit/' + d.id)}>Upraviť</Button>
                                <Button className="idsk-button idsk-button--secondary" style={{marginTop: '10px', marginBottom: '10px'}} onClick={async () => {
                                    if (await removeDataset(d.id)) {
                                        refresh();
                                    }
                                }}>Odstrániť</Button>
                            </TableCell>
                        </TableRow>)}
                    </TableBody>
                </Table>
                <Pagination totalItems={datasets.totalCount} pageSize={query.pageSize} currentPage={query.page} onPageChange={p => setQueryParameters({page: p})} /></> : <div>No datasets found</div>}
            </> : null}
        </MainContent>
        </>;
}