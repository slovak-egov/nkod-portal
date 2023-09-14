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

export default function UserList()
{
    return <>
    <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Lokálne katalógy'}]} />
            <MainContent>
            <PageHeader>Zoznam používateľov</PageHeader>
            <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>Poskytovateľ dát</span><br />
                        Ministerstvo investícií, regionálneho rozvoja a informatizácie Slovenskej republiky 
                    </p>
                    <p>
                <Button>Nový používateľ</Button>
            </p>
            <Table>
                <TableHead>
                    <TableRow>
                        <TableHeaderCell>
                            Meno a priezvisko
                        </TableHeaderCell>
                        <TableHeaderCell>
                            Dátum registrácie
                        </TableHeaderCell>
                        <TableHeaderCell>
                            Rola
                        </TableHeaderCell>
                        <TableHeaderCell>
                            Nástroje
                        </TableHeaderCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    <TableRow>
                        <TableCell>
                            Miroslav Ivan 
                        </TableCell>
                        <TableCell>
                            21. 7. 2023 14:32
                        </TableCell>
                        <TableCell>
                            Zverejňovateľ
                        </TableCell>
                        <TableCell style={{whiteSpace: 'nowrap'}}>
                            <Button className="idsk-button idsk-button--secondary" style={{marginRight: '10px'}}>Upraviť</Button>
                            <Button className="idsk-button idsk-button--secondary" >Odstrániť</Button>
                        </TableCell>
                    </TableRow>
                    <TableRow>
                        <TableCell>
                             Peter Kráľ
                        </TableCell>
                        <TableCell>
                            22. 6. 2023 11:10
                        </TableCell>
                        <TableCell>
                            Administrátor poskytovateľa
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