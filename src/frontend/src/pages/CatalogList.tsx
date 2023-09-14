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

export default function CatalogList()
{
    return <>
    <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Zoznam lokálnych katalógov'}]} />
            <MainContent>
            <PageHeader>Zoznam lokálnych katalógov</PageHeader>
            <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>Poskytovateľ dát</span><br />
                        Ministerstvo investícií, regionálneho rozvoja a informatizácie Slovenskej republiky 
                    </p>
            <p>
                <Button>Nový lokálny katalóg</Button>
            </p>
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
                            Dátum poslednej aktualizácie
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
                            Open data portál
                        </TableCell>
                        <TableCell>
                            21. 7. 2023 14:32
                        </TableCell>
                        <TableCell>
                            24. 7. 2023 10:12
                        </TableCell>
                        <TableCell>
                            Zverejnený
                        </TableCell>
                        <TableCell style={{whiteSpace: 'nowrap'}}>
                            <Button className="idsk-button idsk-button--secondary" style={{marginRight: '10px'}}>Upraviť</Button>
                            <Button className="idsk-button idsk-button--secondary" >Odstrániť</Button>
                        </TableCell>
                    </TableRow>
                </TableBody>
            </Table>
            <Pagination totalItems={81} pageSize={10} currentPage={1} onPageChange={() => {}} />
            </MainContent>
        </>;
}