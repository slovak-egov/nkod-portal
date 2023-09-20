import { useState } from "react";

import PageHeader from "../components/PageHeader";
import Radio from "../components/Radio";
import MultiCheckbox from "../components/MultiCheckbox";
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
import { useNavigate, useParams } from "react-router";
import { removeDistribution, useDataset, useDistributions, useUserInfo } from "../client";

export default function DistributionList()
{
    const { datasetId } = useParams();
    const [distributions, query, setQueryParameters, loading, error, refresh] = useDistributions(datasetId ? {filters: {parent: [datasetId]}} : {page: 0});
    const [userInfo] = useUserInfo();
    const [dataset] = useDataset(datasetId);
    const navigate = useNavigate();

    return <>
            <Breadcrumbs items={[{title: 'Národný katalóg otvorených dát', link: '/'},{title: 'Zoznam datasetov', link: '/admin/datasets'}, {title: 'Organizačná štruktúra júl 2023', link: '/admin/datasets'}, {title: 'Distribúcie'}]} />
            <MainContent>
            <PageHeader>Zoznam distribúcií</PageHeader>
            {userInfo?.publisherView ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>Poskytovateľ dát</span><br />
                        {userInfo.publisherView.name}
                    </p> : null}
                    {dataset ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>Dataset</span><br />
                    {dataset.name}
                    </p> : null}
            <p>
                <Button onClick={() => navigate('/sprava/distribucie/' + datasetId + '/pridat')}>Nová distribúcia</Button>
            </p>
            {distributions ? <><Table>
                <TableHead>
                    <TableRow>
                        <TableHeaderCell>
                            Formát
                        </TableHeaderCell>
                        <TableHeaderCell>
                            Nástroje
                        </TableHeaderCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    {distributions.items.map(d => <TableRow key={d.id}>
                        <TableCell>
                            {d.downloadUrl ? <a href={d.downloadUrl} className="govuk-link">{d.title ?? d.formatValue?.label ?? d.id}</a> : <span></span>}
                        </TableCell>
                        <TableCell style={{whiteSpace: 'nowrap'}}>
                            {d.downloadUrl ? <Button className="idsk-button idsk-button--secondary" style={{marginRight: '10px'}} onClick={() => { if (d.downloadUrl) {window.location.href = d.downloadUrl}}}>Stiahnuť</Button> : null}
                            <Button className="idsk-button idsk-button--secondary" style={{marginRight: '10px'}} onClick={() => navigate('/sprava/distribucie/' + datasetId + '/upravit/' + d.id)}>Upraviť</Button>
                            <Button className="idsk-button idsk-button--secondary" onClick={async () => {
                                    if (await removeDistribution(d.id)) {
                                        refresh();
                                    }
                                }}>Odstrániť</Button>
                        </TableCell>
                    </TableRow>)}
                </TableBody>
            </Table>
            <Pagination totalItems={distributions.totalCount} pageSize={query.pageSize} currentPage={query.page} onPageChange={p => setQueryParameters({page: p})} /></> : <div>No distributions found</div>}
            </MainContent>
        </>;
}