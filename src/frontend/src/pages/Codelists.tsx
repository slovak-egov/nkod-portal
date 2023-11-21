import PageHeader from "../components/PageHeader";
import Table from "../components/Table";
import TableHead from "../components/TableHead";
import TableRow from "../components/TableRow";
import TableHeaderCell from "../components/TableHeaderCell";
import TableBody from "../components/TableBody";
import TableCell from "../components/TableCell";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import { useCodelistAdmin, useCodelistFileUpload, useDocumentTitle } from "../client";
import FormElementGroup from "../components/FormElementGroup";
import FileUpload from "../components/FileUpload";
import Alert from "../components/Alert";
import Loading from "../components/Loading";
import ErrorAlert from "../components/ErrorAlert";
import { useTranslation } from "react-i18next";

export default function Codelists()
{
    const [codelists, loading, error, refresh] = useCodelistAdmin();
    const [ uploading, upload ] = useCodelistFileUpload();
    const {t} = useTranslation();
    useDocumentTitle(t('codelists'));

    return <>
        <Breadcrumbs items={[{title: t('nkod'), link: '/'}, {title: t('codelists')}]} />
            <MainContent>
            <PageHeader>{t('codelists')}</PageHeader>

            <FormElementGroup label={t('codelistFileUpload')} element={id => <FileUpload id={id} onChange={async e => {
                const files = e.target.files ?? [];
                if (files.length > 0) {
                    await upload(files[0]);
                    refresh();
                }
            }} />} />

            {uploading ? <Alert type="info">
                {t('fileUploadProgress')}
            </Alert> : null}

            {loading ? <Loading /> : null}
            {error ? <ErrorAlert error={error} /> : null}

            {codelists && codelists.length > 0 ? <Table>
                <TableHead>
                    <TableRow>
                        <TableHeaderCell>
                            Id
                        </TableHeaderCell>
                        <TableHeaderCell>
                            {t('name')}
                        </TableHeaderCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    {codelists.map(c => <TableRow key={c.id}>
                        <TableCell>
                            {c.id}
                        </TableCell>
                        <TableCell>
                            {c.label}
                        </TableCell>
                    </TableRow>)}
                </TableBody>
            </Table> : null}
            </MainContent>
        </>;
}