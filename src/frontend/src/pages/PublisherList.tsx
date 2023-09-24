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
import { sendPut, removeEntity, usePublishers, useDefaultHeaders } from "../client";

export default function PublisherList()
{
    const [publishers, query, setQueryParameters, loading, error, refresh] = usePublishers();
    const headers = useDefaultHeaders();

    async function updatePublisherStatus(publisherId: string, isEnabled: boolean) {
        try {
            await sendPut('publishers', {
                publisherId: publisherId,
                isEnabled: isEnabled
            }, headers);
            refresh();
        } catch (e) {

        }
    }

    return <>
    <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Poskytovatelia dát'}]} />
            <MainContent>
            <PageHeader>Poskytovatelia dát</PageHeader>
            {publishers && publishers.items.length > 0 ? <><Table>
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
                    {publishers.items.map(p => <TableRow key={p.id}>
                        <TableCell>
                            {p.name}
                        </TableCell>
                        <TableCell>
                            {p.isPublic ? 'publikovaný' : 'nepublikovaný'}
                        </TableCell>
                        <TableCell >
                            <Button className="idsk-button idsk-button--secondary" style={{marginRight: '10px'}} onClick={() => updatePublisherStatus(p.id, !p.isPublic)}>{p.isPublic ? 'Deaktivovať' : 'Aktivovať'}</Button>
                            <Button className="idsk-button idsk-button--secondary" style={{marginRight: '10px'}}>Impersonovať</Button>
                            <Button className="idsk-button idsk-button--secondary" onClick={async () => {
                                await removeEntity('publishers', p.id, headers);
                                refresh();
                            }} >Odstrániť</Button>
                        </TableCell>
                    </TableRow>)}
                </TableBody>
            </Table>
            <Pagination totalItems={publishers.totalCount} pageSize={query.pageSize} currentPage={query.page} onPageChange={p => setQueryParameters({page: p})} />
            </> 
            : <div>No publishers found</div>}
            </MainContent>
        </>;
}