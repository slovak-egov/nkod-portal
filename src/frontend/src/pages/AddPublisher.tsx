import PageHeader from "../components/PageHeader";
import Button from "../components/Button";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import FormElementGroup from "../components/FormElementGroup";
import BaseInput from "../components/BaseInput";
import { useDocumentTitle, usePublisherAdd, useUserInfo } from "../client";
import { useNavigate } from "react-router";
import { useTranslation } from "react-i18next";

export default function AddPublisher()
{
    const [userInfo] = useUserInfo();
    const [publisher, setPublisher, errors, saving, save] = usePublisherAdd({
        website: '',
        email: '',
        phone: ''
    });
    const navigate = useNavigate();
    const {t} = useTranslation();
    useDocumentTitle(t('publisherRegistration'));

    return <>
    <Breadcrumbs items={[{title: t('nkod'), link: '/'},{title: t('publisherRegistration')}]} />
            <MainContent>
            <PageHeader>{t('publisherRegistration')}</PageHeader>

                    <FormElementGroup label={t('websiteAddress')} errorMessage={errors['website']} element={id => <BaseInput id={id} disabled={saving} value={publisher.website} onChange={e => setPublisher({website: e.target.value})} />} />
                    <FormElementGroup label={t('contantEmailAddress')} errorMessage={errors['email']} element={id => <BaseInput id={id} disabled={saving} value={publisher.email} onChange={e => setPublisher({email: e.target.value})} />} />
                    <FormElementGroup label={t('contactPhoneNumber')} errorMessage={errors['phone']} element={id => <BaseInput id={id} disabled={saving} value={publisher.phone} onChange={e => setPublisher({phone: e.target.value})} />} />
                    
                    <Button style={{marginRight: '20px'}} onClick={async () => {
                        const result = await save();
                        if (result?.success) {
                            navigate('/sprava/caka-na-schvalenie');
                        }
                    }} disabled={saving}>
                        {t('register')}
                    </Button>
            </MainContent>
        </>;
}