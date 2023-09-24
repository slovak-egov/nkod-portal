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
import { removeLocalCatalog, useDefaultHeaders, useLocalCatalogs, useUserInfo } from "../client";
import { useNavigate } from "react-router";
import { useEffect } from "react";

export default function CatalogList()
{
    const [catalogs, query, setQueryParameters, loading, error, refresh] = useLocalCatalogs({pageSize: 20, page: 0});
    const navigate = useNavigate();
    const [userInfo] = useUserInfo();
    const headers = useDefaultHeaders();

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
    <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Zoznam lokálnych katalógov'}]} />
            <MainContent>
            <PageHeader>Zoznam lokálnych katalógov</PageHeader>
            {userInfo?.publisherView ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>Poskytovateľ dát</span><br />
                        {userInfo.publisherView.name}
                    </p> : null}
            <p>
                <Button onClick={() => navigate('/sprava/lokalne-katalogy/pridat')}>Nový lokálny katalóg</Button>
            </p>
            {catalogs ? <><Table>
                <TableHead>
                    <TableRow>
                        <TableHeaderCell>
                            Názov
                        </TableHeaderCell>
                        <TableHeaderCell>
                            Stav
                        </TableHeaderCell>
                        <TableHeaderCell>
                            Nástroje
                        </TableHeaderCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    {catalogs.items.map(c => <TableRow key={c.id}>
                        <TableCell>
                            {c.name}
                        </TableCell>
                        <TableCell>
                            {c.isPublic ? 'publikovaný' : 'nepublikovaný'}
                        </TableCell>
                        <TableCell style={{whiteSpace: 'nowrap'}}>
                            <Button className="idsk-button idsk-button--secondary" style={{marginRight: '10px'}} onClick={() => navigate('/sprava/lokalne-katalogy/upravit/' + c.id)}>Upraviť</Button>
                            <Button className="idsk-button idsk-button--secondary" onClick={async () => {
                                    if (await removeLocalCatalog(c.id, headers)) {
                                        refresh();
                                    }
                                }}>Odstrániť</Button>
                        </TableCell>
                    </TableRow>)}
                </TableBody>
            </Table>
            <Pagination totalItems={catalogs.totalCount} pageSize={query.pageSize} currentPage={query.page} onPageChange={p => setQueryParameters({page: p})} /></> : <div>No catalogs found</div>}
            </MainContent>
        </>;
}