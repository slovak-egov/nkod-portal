import { LocalCatalog, LocalCatalogInput, useDocumentTitle, useLocalCatalogEdit, useUserInfo } from "../client";

import PageHeader from "../components/PageHeader";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import Button from "../components/Button";
import ValidationSummary from "../components/ValidationSummary";
import Loading from "../components/Loading";
import { LocalCatalogForm } from "../components/LocalCatalogForm";
import { useNavigate } from "react-router";
import { useTranslation } from "react-i18next";

const transformEntityForEdit = (entity: LocalCatalog): LocalCatalogInput => {
    return {
        id: entity.id,
        isPublic: entity.isPublic,
        name: entity.nameAll ?? {},
        description: entity.descriptionAll ?? {},
        contactName: entity?.contactPoint?.nameAll ?? {},
        contactEmail: entity.contactPoint?.email ?? '',
        homePage: entity.homePage,
        type: entity.type,
        endpointUrl: entity.endpointUrl
    }
};

export default function EditCatalog()
{
    const [inputs, , loading, setCatalog, errors, saving, save] = useLocalCatalogEdit(transformEntityForEdit);

    const [userInfo] = useUserInfo();
    const navigate = useNavigate();
    const {t} = useTranslation();
    useDocumentTitle(t('editCatalog'));

    return <>
            <Breadcrumbs items={[{title: t('nkod'), link: '/'}, {title: t('localCatalogList'), link: '/sprava/lokalne-katalogy'}, {title: t('editCatalog')}]} />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>{t('editCatalog')}</PageHeader>
                    {userInfo?.publisherView ? <p className="govuk-body nkod-publisher-name">
                    <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>{t('publisher')}</span><br />
                        {userInfo.publisherView.name}
                    </p> : null}

                    {Object.keys(errors).length > 0 ? <ValidationSummary elements={Object.entries(errors).map(k => ({
                        elementId: k[0],
                        message: k[1]
                    }))} /> : null}

                    {
                        !loading && inputs ? <>
                        <LocalCatalogForm catalog={inputs} 
                                          setCatalog={setCatalog} 
                                          errors={errors}
                                          saving={saving} />
                        <Button style={{marginRight: '20px'}} onClick={async () => {
                        const result = await save();
                        if (result?.success) {
                            navigate('/sprava/lokalne-katalogy');
                        }
                    }} disabled={saving}>
                            {t('save')}
                        </Button></> : <Loading />
                    }
                </div>
            </MainContent>
        </>;
}