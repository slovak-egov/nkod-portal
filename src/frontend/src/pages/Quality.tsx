import { useState } from "react";

import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import Table from "../components/Table";
import TableHead from "../components/TableHead";
import TableRow from "../components/TableRow";
import TableHeaderCell from "../components/TableHeaderCell";
import TableBody from "../components/TableBody";
import TableCell from "../components/TableCell";
import PageSubheader from "../components/PageSubHeader";

export default function Quality()
{
    const [selectedItems, setSelectedItems] = useState<string[]>([]);

    return <>
            <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Kvalita metadát'}]} />
            <MainContent>
                <PageHeader>Kvalita metadát</PageHeader>
                <PageSubheader style={{color: '#2B8CC4', margin: '30px 0 20px 0'}}>Počet distribúcií bez uvedenia licencie použitia podľa poskytovateľa</PageSubheader>
                <Table>
                <TableHead>
                    <TableRow>
                        <TableHeaderCell enableSorting>
                            Poskytovateľ
                        </TableHeaderCell>
                        <TableHeaderCell enableSorting>
                            Distribúcie bez licencií
                        </TableHeaderCell>
                        <TableHeaderCell enableSorting>
                            Celkový počet distribúcií
                        </TableHeaderCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    <TableRow>
                        <TableCell>
                            Ministerstvo investícií, regionálneho rozvoja a informatizácie Slovenskej republiky 
                        </TableCell>
                        <TableCell>
                            3 076
                        </TableCell>
                        <TableCell>
                            217 398
                        </TableCell>
                    </TableRow>
                    <TableRow>
                        <TableCell>
                            Ministerstvo životného prostredia Slovenskej republiky
                        </TableCell>
                        <TableCell>
                            1 623
                        </TableCell>
                        <TableCell>
                            81 518
                        </TableCell>
                    </TableRow>
                </TableBody>
            </Table>
            <PageSubheader style={{color: '#2B8CC4', margin: '30px 0 20px 0'}}>Počet datasetov bez uvedenia licencie použitia podľa poskytovateľa</PageSubheader>
                <Table>
                <TableHead>
                    <TableRow>
                        <TableHeaderCell enableSorting>
                            Poskytovateľ
                        </TableHeaderCell>
                        <TableHeaderCell enableSorting>
                            Datasetov bez licencie
                        </TableHeaderCell>
                        <TableHeaderCell enableSorting>
                            Celkový počet datasetov
                        </TableHeaderCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    <TableRow>
                        <TableCell>
                            Ministerstvo investícií, regionálneho rozvoja a informatizácie Slovenskej republiky 
                        </TableCell>
                        <TableCell>
                            3 076
                        </TableCell>
                        <TableCell>
                            217 398
                        </TableCell>
                    </TableRow>
                    <TableRow>
                        <TableCell>
                            Ministerstvo životného prostredia Slovenskej republiky
                        </TableCell>
                        <TableCell>
                            1 623
                        </TableCell>
                        <TableCell>
                            81 518
                        </TableCell>
                    </TableRow>
                </TableBody>
            </Table>
            <PageSubheader style={{color: '#2B8CC4', margin: '30px 0 20px 0'}}>Počet záznamov datasetov nespĺňajúcich povinné atribúty podľa poskytovateľa</PageSubheader>
                <Table>
                <TableHead>
                    <TableRow>
                        <TableHeaderCell enableSorting>
                            Poskytovateľ
                        </TableHeaderCell>
                        <TableHeaderCell enableSorting>
                            Počet datasetov s chýbajúcimi atribútami
                        </TableHeaderCell>
                        <TableHeaderCell enableSorting>
                            Celkový počet datasetov
                        </TableHeaderCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    <TableRow>
                        <TableCell>
                            Ministerstvo investícií, regionálneho rozvoja a informatizácie Slovenskej republiky 
                        </TableCell>
                        <TableCell>
                            3 076
                        </TableCell>
                        <TableCell>
                            217 398
                        </TableCell>
                    </TableRow>
                    <TableRow>
                        <TableCell>
                            Ministerstvo životného prostredia Slovenskej republiky
                        </TableCell>
                        <TableCell>
                            1 623
                        </TableCell>
                        <TableCell>
                            81 518
                        </TableCell>
                    </TableRow>
                </TableBody>
            </Table>
            <PageSubheader style={{color: '#2B8CC4', margin: '30px 0 20px 0'}}>Počet datasetov podľa formátu</PageSubheader>
                <Table>
                <TableHead>
                    <TableRow>
                        <TableHeaderCell enableSorting>
                            Formát
                        </TableHeaderCell>
                        <TableHeaderCell enableSorting>
                            Celkový počet datasetov
                        </TableHeaderCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    <TableRow>
                        <TableCell>
                            CSV
                        </TableCell>
                        <TableCell>
                            9 761
                        </TableCell>
                    </TableRow>
                    <TableRow>
                        <TableCell>
                            XML
                        </TableCell>
                        <TableCell>
                            7 015
                        </TableCell>
                    </TableRow>
                </TableBody>
            </Table>
            </MainContent>
        </>;
}