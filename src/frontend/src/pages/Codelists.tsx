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
import { useCodelistAdmin, useCodelistFileUpload } from "../client";
import FormElementGroup from "../components/FormElementGroup";
import FileUpload from "../components/FileUpload";
import Alert from "../components/Alert";

export default function Codelists()
{
    const [codelists, loading, error, refresh] = useCodelistAdmin();
    const [ uploading, upload ] = useCodelistFileUpload();

    return <>
        <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Číselníky'}]} />
            <MainContent>
            <PageHeader>Číselníky</PageHeader>

            <p><FormElementGroup label="Upload súboru číselníka" element={id => <FileUpload id={id} onChange={async e => {
                const files = e.target.files ?? [];
                if (files.length > 0) {
                    const file = await upload(files[0]);
                    refresh();
                }
            }} />} /></p>

            {uploading ? <Alert type="info">
                Prebieha upload súboru
            </Alert> : null}

            {codelists && codelists.length > 0 ? <Table>
                <TableHead>
                    <TableRow>
                        <TableHeaderCell>
                            Id
                        </TableHeaderCell>
                        <TableHeaderCell>
                            Názov
                        </TableHeaderCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    {codelists.map(c => <TableRow key={c.id}>
                        <TableCell>
                            {c.id}
                        </TableCell>
                        <TableCell>
                            {c.name}
                        </TableCell>
                    </TableRow>)}
                </TableBody>
            </Table> : null}
            </MainContent>
        </>;
}