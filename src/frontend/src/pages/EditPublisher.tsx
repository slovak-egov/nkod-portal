import { AdminPublisherInput, Publisher, useDocumentTitle, useEntityEdit } from '../client';

import PageHeader from '../components/PageHeader';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import Button from '../components/Button';
import ValidationSummary from '../components/ValidationSummary';
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { AdminPublisherForm } from '../components/AdminPublisherForm';
import Loading from '../components/Loading';

const transformEntityForEdit = (entity: Publisher): AdminPublisherInput => {
    return {
        id: entity.id,
        name: entity.nameAll ?? { sk: '' },
        website: entity.website ?? '',
        email: entity.email ?? '',
        phone: entity.phone ?? '',
        legalForm: entity.legalForm ?? '',
        uri: entity.key ?? '',
        isEnabled: entity.isPublic
    };
};

export default function EditPublisher() {
    const [inputs, , loading, setPublisher, errors, saving, save] = useEntityEdit<Publisher, AdminPublisherInput>(
        'publishers',
        'publishers/search',
        transformEntityForEdit
    );

    const navigate = useNavigate();
    const { t } = useTranslation();
    useDocumentTitle(t('editPublisher'));

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('publishers'), link: '/sprava/poskytovatelia' }, { title: t('editPublisher') }]} />
            <MainContent>
                <div className="nkod-form-page">
                    <PageHeader>{t('editPublisher')}</PageHeader>

                    {Object.keys(errors).length > 0 ? (
                        <ValidationSummary
                            elements={Object.entries(errors).map((k) => ({
                                elementId: k[0],
                                message: k[1]
                            }))}
                        />
                    ) : null}

                    {!loading && inputs ? (
                        <>
                            <AdminPublisherForm publisher={inputs} setPublisher={setPublisher} errors={errors} saving={saving} />

                            <Button
                                style={{ marginRight: '20px' }}
                                onClick={async () => {
                                    const result = await save();
                                    if (result?.success) {
                                        navigate('/sprava/poskytovatelia');
                                    }
                                }}
                                disabled={saving}
                            >
                                {t('save')}
                            </Button>
                        </>
                    ) : (
                        <Loading />
                    )}
                </div>
            </MainContent>
        </>
    );
}
