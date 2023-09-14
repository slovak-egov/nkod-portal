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
import { usePublishers } from "../client";

export default function PublisherList()
{
    const [publishers, loading, error] = usePublishers();

    return <>
    <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Poskytovatelia dát'}]} />
            <MainContent>
            <PageHeader>Poskytovatelia dát</PageHeader>
            <Table>
                <TableHead>
                    <TableRow>
                        <TableHeaderCell>
                            Názov
                        </TableHeaderCell>
                        <TableHeaderCell>
                            Dátum vytvorenia
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
                    <TableRow>
                        <TableCell>
                        Ministerstvo investícií, regionálneho rozvoja a informatizácie Slovenskej republiky 
                        </TableCell>
                        <TableCell>
                            21. 7. 2023
                        </TableCell>
                        <TableCell>
                            Zverejnený
                        </TableCell>
                        <TableCell >
                            <Button className="idsk-button idsk-button--secondary" style={{marginRight: '10px'}}>Deaktivovať</Button>
                            <Button className="idsk-button idsk-button--secondary" style={{marginRight: '10px'}}>Impersonovať</Button>
                            <Button className="idsk-button idsk-button--secondary" >Odstrániť</Button>
                        </TableCell>
                    </TableRow>
                </TableBody>
            </Table>
            <Pagination totalItems={81} pageSize={10} currentPage={1} onPageChange={() => {}} />
            </MainContent>
        </>;
}