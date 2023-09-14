import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import GridRow from "../components/GridRow";
import GridColumn from "../components/GridColumn";
import RelatedContent from "../components/RelatedContent";

import csvIcon from '../icons/csv.png';
import docIcon from '../icons/doc.png';
import htmlIcon from '../icons/html.png';
import jsonIcon from '../icons/json.png';
import mdbIcon from '../icons/mdb.png';
import odsIcon from '../icons/ods.png';
import pdfIcon from '../icons/pdf.png';
import rdfIcon from '../icons/rdf.png';
import sqlIcon from '../icons/sql.png';
import txtIcon from '../icons/txt.png';
import xlsIcon from '../icons/xls.png';
import xlsxIcon from '../icons/xlsx.png';
import xmlIcon from '../icons/xml.png';

import Loading from "../components/Loading";
import ErrorAlert from "../components/ErrorAlert";
import { useDataset, useDatasets } from "../client";
import { useParams } from "react-router";

export default function DetailDataset()
{
    const [dataset, loading, error] = useDataset();
    const { id } = useParams();
    const [datasets] = useDatasets(id ? {filters: {sibling: [id]}} : {pageSize: 0});

    const path = [{title: 'Národný katalóg otvorených dát', link: '/'}, {title: 'Vyhľadávanie', link: '/datasety'}];
    if (dataset?.name) {
        path.push({title: dataset.name, link: '/datasety/' + dataset.id});
    }

    return  <>{loading ? <Loading /> : error ? <ErrorAlert error={error} /> : dataset ?
        <><Breadcrumbs items={path} />
        <MainContent>
            <div className="nkod-entity-detail">
                <PageHeader>{dataset.name}</PageHeader>
                {dataset.publisher ? <p className="govuk-body nkod-publisher-name">
                    {dataset.publisher.name}
                </p> : null}
                {dataset.description ? <p className="govuk-body nkod-entity-description">
                    {dataset.description}
                </p> : null}
                {dataset.themes.length > 0 ? <div className="nkod-entity-detail-tags govuk-clearfix">
                    {dataset.themes.map(t => <div key={t} className="govuk-body nkod-entity-detail-tag">
                        {t}
                    </div>)}
                </div> : null}
                <GridRow>
                    {dataset.themeValues.length > 0 ? <GridColumn widthUnits={1} totalUnits={4}>
                        <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                Téma
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value">
                                {dataset.themeValues.map(l => <span key={l.label}>{l.label}</span>).join(', ')}
                            </div>
                        </div>                        
                    </GridColumn> : null}          
                    {dataset.documentation ? <GridColumn widthUnits={1} totalUnits={4}>
                        <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                Dokumentácia
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value">
                                <a href={dataset.documentation} className="govuk-link">Zobraziť dokumentáciu</a>
                            </div>
                        </div>                        
                    </GridColumn> : null}          
                    {dataset.accrualPeriodicityValue ? <GridColumn widthUnits={1} totalUnits={4}>
                        <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                Periodicita aktualizácie
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value">
                                mesačná
                            </div>
                        </div>
                    </GridColumn> : null}
                    {dataset.contactPoint?.name || dataset.contactPoint?.email ? <GridColumn widthUnits={1} totalUnits={4}>
                        <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                Kontaktný bod
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value">
                                {dataset.contactPoint?.name ? <div>
                                    {dataset.contactPoint.name}
                                </div> : null}
                                {dataset.contactPoint?.email ? <div>
                                    {dataset.contactPoint.email}
                                </div> : null}
                            </div>
                        </div>                        
                    </GridColumn> : null}
                </GridRow>
                <GridRow>
                    <GridColumn widthUnits={1} totalUnits={4}>
                        {dataset.spatialValues.length > 0 ? <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                Územná platnosť
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value">
                                {dataset.spatialValues.map(l => <span key={l.label}>{l.label}</span>).join(', ')}
                            </div>
                        </div> : null}
                        {dataset.temporal?.startDate || dataset.temporal?.endDate ? <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                Časová platnosť
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value">
                                {dataset.temporal?.startDate ? <div>
                                    <span style={{fontWeight: 'bold'}}>od: </span> {dataset.temporal.startDate}
                                </div> : null}
                                {dataset.temporal?.endDate ? <div>
                                    <span style={{fontWeight: 'bold'}}>do: </span> {dataset.temporal.endDate}
                                </div> : null}
                            </div>
                        </div> : null}
                    </GridColumn>
                    {dataset.distributions.length > 0 ? <GridColumn widthUnits={3} totalUnits={4}>
                        <div className="govuk-body nkod-detail-distribution-count">
                            {dataset.distributions.length} {dataset.distributions.length === 1 ? 'distribúcia' : dataset.distributions.length < 5 ? 'distribúcie' : 'distribúcií'}
                        </div>
                        <hr className="idsk-crossroad-line" aria-hidden="true"/>
                        {dataset.distributions.map(distrubution => <div key={distrubution.id} className="govuk-body nkod-detail-distribution-row">
                            <span className="govuk-body nkod-detail-distribution-format">
                                {
                                    distrubution.formatValue?.label?.toLowerCase() === 'csv' ? <img src={csvIcon} alt="csv" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'doc' ? <img src={docIcon} alt="doc" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'html' ? <img src={htmlIcon} alt="html" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'json' ? <img src={jsonIcon} alt="json" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'mdb' ? <img src={mdbIcon} alt="mdb" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'ods' ? <img src={odsIcon} alt="ods" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'pdf' ? <img src={pdfIcon} alt="pdf" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'rdf' ? <img src={rdfIcon} alt="rdf" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'sql' ? <img src={sqlIcon} alt="sql" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'txt' ? <img src={txtIcon} alt="txt" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'xls' ? <img src={xlsIcon} alt="xls" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'xlsx' ? <img src={xlsxIcon} alt="xlsx" /> :
                                    distrubution.formatValue?.label?.toLowerCase() === 'xml' ? <img src={xmlIcon} alt="xml" /> : null
                                }
                            </span>
                            <span className="govuk-body nkod-detail-distribution-url">
                                {distrubution.downloadUrl ? <a href={distrubution.downloadUrl} className="govuk-link">
                                    {distrubution.title ?? dataset.name}
                                </a> : null}
                            </span>
                            <hr className="idsk-crossroad-line" aria-hidden="true"/>
                        </div>)}
                    </GridColumn> : null}
                </GridRow>
                {(datasets && datasets.items.length > 0) ? <GridRow>
                    <GridColumn widthUnits={1} totalUnits={1}>
                        <RelatedContent header="Dalšie datasety z tejto série" links={datasets.items.map(d => ({
                            title: d.name ?? 'Bez názvu',
                            url: '/datasety/' + d.id
                        }))}  />
                    </GridColumn>
                </GridRow> : null}
            </div>
        </MainContent></> : null}
    </>
}