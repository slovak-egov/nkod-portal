import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import GridRow from "../components/GridRow";
import GridColumn from "../components/GridColumn";
import { useDocumentTitle, useLocalCatalog } from "../client";
import Loading from "../components/Loading";
import ErrorAlert from "../components/ErrorAlert";
import { useTranslation } from "react-i18next";

export default function DetailLocalCatalog()
{
    const [catalog, loading, error] = useLocalCatalog();
    const {t} = useTranslation();
    useDocumentTitle(catalog?.name ?? '');

    return  <>
        {loading ? <Loading /> : error ? <ErrorAlert error={error} /> : catalog ?
        <><Breadcrumbs items={[{title: t('nkod'), link: '/'}, {title: t('localCatalogList'), link: '/lokalne-katalogy'}, {title: catalog.name}]} />
        <MainContent>
            <div className="nkod-entity-detail">
                <PageHeader>{catalog.name}</PageHeader>
                {catalog.publisher ? <p className="govuk-body nkod-publisher-name" data-testid="publisher-name">
                    {catalog.publisher.name}
                </p> : null}
                {catalog.description ? <p className="govuk-body nkod-entity-description" data-testid="description">
                    {catalog.description}
                </p> : null}
                <GridRow>
                    {catalog.publisher ? <GridColumn widthUnits={1} totalUnits={4}>
                        <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                {t('publisher')}
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value" data-testid="publisher-name" style={{wordBreak: 'break-word'}}>
                                {catalog.publisher.name}
                            </div>
                        </div>                        
                    </GridColumn> : null}
                   
                    {catalog.homePage ? <GridColumn widthUnits={1} totalUnits={4}>
                        <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                {t('homePagePublisher')}
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value" style={{wordBreak: 'break-word'}}>
                                <a href={catalog.homePage} className="govuk-link" data-testid="homepage">{t('goToHomePageOfCatalog')}</a>
                            </div>
                        </div>                        
                    </GridColumn> : null}
                    
                    {catalog.typeValue ? <GridColumn widthUnits={1} totalUnits={4}>
                        <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                {t('catalogType')}
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value" data-testid="local-catalog-type" style={{wordBreak: 'break-word'}}>
                                {catalog.typeValue.label}
                            </div>
                        </div>
                    </GridColumn> : null}

                    {catalog.endpointUrl ? <GridColumn widthUnits={1} totalUnits={4}>
                        <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                {t('catalogEndpoint')}
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value" style={{wordBreak: 'break-word'}}>
                                {catalog.endpointUrl}
                            </div>
                        </div>                        
                    </GridColumn> : null}

                    {catalog.contactPoint?.name || catalog.contactPoint?.email ? <GridColumn widthUnits={1} totalUnits={4}>
                        <div className="nkod-detail-attribute">
                            <div className="govuk-body nkod-detail-attribute-name">
                                {t('contactPoint')}
                            </div>
                            <div className="govuk-body nkod-detail-attribute-value" style={{wordBreak: 'break-word'}}>
                                {catalog.contactPoint?.name ? <div>
                                    <span data-testid="contact-name">{catalog.contactPoint.name}</span>
                                </div> : null}
                                {catalog.contactPoint?.email ? <div>
                                    <span data-testid="contact-email">{catalog.contactPoint.email}</span>
                                </div> : null}
                            </div>
                        </div>                        
                    </GridColumn> : null}
                </GridRow>
            </div>
        </MainContent></> : null}
    </>
}