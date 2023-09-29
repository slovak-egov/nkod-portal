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
import { removeUser, useDefaultHeaders, useDocumentTitle, useUserInfo, useUsers } from "../client";
import { useNavigate } from "react-router";
import RoleName from "../components/RoleName";
import Loading from "../components/Loading";
import ErrorAlert from "../components/ErrorAlert";
import { useTranslation } from "react-i18next";

export default function UserList()
{
    const [users, query, setQueryParameters, loading, error, refresh] = useUsers();
    const [userInfo] = useUserInfo();
    const navigate = useNavigate();
    const headers = useDefaultHeaders();
    const {t} = useTranslation();
    useDocumentTitle(t('userList'));

    return <>
        <Breadcrumbs items={[{title: t('nkod'), link: '/'}, {title: t('userList')}]} />
            <MainContent>
            <PageHeader>{t('userList')}</PageHeader>
            {userInfo?.publisherView ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>{t('publisher')}</span><br />
                        {userInfo.publisherView.name}
                    </p> : null}
            <p>
                <Button onClick={() => navigate('/sprava/pouzivatelia/pridat')}>{t('newUser')}</Button>
            </p>

            {loading ? <Loading /> : null}
            {error ? <ErrorAlert error={error} /> : null}

            {users && users.items.length > 0 ? <><Table>
                <TableHead>
                    <TableRow>
                        <TableHeaderCell>
                            {t('firstNameAndLastName')}
                        </TableHeaderCell>
                        <TableHeaderCell>
                            {t('role')}
                        </TableHeaderCell>
                        <TableHeaderCell>
                            {t('tools')}
                        </TableHeaderCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    {users.items.map(u => <TableRow key={u.id}>
                        <TableCell>
                            {u.firstName} {u.lastName}
                        </TableCell>
                        <TableCell>
                            <RoleName role={u.role} />
                        </TableCell>
                        <TableCell style={{whiteSpace: 'nowrap'}}>
                            <Button className="idsk-button idsk-button--secondary" style={{marginRight: '10px'}} onClick={() => navigate('/sprava/pouzivatelia/upravit/' + u.id)}>{t('edit')}</Button>
                            <Button className="idsk-button idsk-button--secondary" onClick={async () => {
                                    if (await removeUser(u.id, headers)) {
                                        refresh();
                                    }
                                }}>{t('remove')}</Button>
                        </TableCell>
                    </TableRow>)}
                </TableBody>
            </Table>
            <Pagination totalItems={users.totalCount} pageSize={query.pageSize} currentPage={query.page} onPageChange={p => setQueryParameters({page: p})} />
            </> : <div className="govuk-body">{t('usersListEmpty')}</div>}
            </MainContent>
        </>;
}