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

export default function CodebookList()
{
    return <>
    <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Číselníky'}]} />
            <MainContent>
            <PageHeader>Číselníky</PageHeader>
            <p>
                <Button>Vložiť súbor s číselníkmi</Button>
            </p>
            <Table>
                <TableHead>
                    <TableRow>
                        <TableHeaderCell>
                            Názov
                        </TableHeaderCell>
                        <TableHeaderCell>
                            Nástroje
                        </TableHeaderCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    <TableRow>
                        <TableCell>
                            http://publications.europa.eu/resource/authority/data-theme
                        </TableCell>
                        <TableCell >
                            <Button className="idsk-button idsk-button--secondary" style={{marginRight: '10px'}}>Stiahnuť</Button>
                        </TableCell>
                    </TableRow>
                </TableBody>
            </Table>
            <Pagination totalItems={81} pageSize={10} currentPage={1} onPageChange={() => {}} />
            </MainContent>
        </>;
}