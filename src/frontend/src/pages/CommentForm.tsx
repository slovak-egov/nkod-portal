import { yupResolver } from '@hookform/resolvers/yup';
import { SubmitHandler, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { buildYup } from 'schema-to-yup';
import { useDefaultHeaders, useUserInfo } from '../client';
import { sendCmsPost } from '../cms';
import Button from '../components/Button';
import FormElementGroup from '../components/FormElementGroup';
import TextArea from '../components/TextArea';
import { ROOT_ID, useSchemaConfig } from '../helpers/helpers';
import { CommentFormValues } from '../interface/cms.interface';
import { schema } from './schemas/CommentSchema';

type Props = {
    contentId?: string;
    parentId?: string;
    datasetUri?: string;
    refresh: (newContentId?: string) => void;
};

export default function CommentForm(props: Props) {
    const { t } = useTranslation();
    const [userInfo] = useUserInfo();
    const headers = useDefaultHeaders();
    const { contentId, refresh, parentId, datasetUri } = props;
    const yupSchema = buildYup(schema, useSchemaConfig(schema.required));

    const form = useForm<CommentFormValues>({
        resolver: yupResolver(yupSchema),
        defaultValues: {
            body: ''
        }
    });

    const {
        register,
        handleSubmit,
        reset,
        formState: { errors }
    } = form;

    const onSubmit: SubmitHandler<CommentFormValues> = async (data) => {
        let result;
        if (!contentId && datasetUri) {
            const request = {
                datasetUri,
                userId: userInfo?.id,
                email: userInfo?.email,
                body: data.body
            };
            result = await sendCmsPost<any>(`datasets/comments`, request, headers);
            if (result?.status === 200) {
                // post datasets/comments returns new CmsDataset, which will be used for
                refresh(result.data);
                reset();
            }
        } else {
            const request = {
                contentId,
                parentId: parentId ?? ROOT_ID,
                userId: userInfo?.id,
                email: userInfo?.email,
                body: data.body
            };
            result = await sendCmsPost<any>(`comments`, request, headers);
            if (result?.status === 200) {
                refresh();
                reset();
            }
        }
    };

    const onErrors = () => {
        console.error(errors);
    };

    return (
        <>
            <form onSubmit={handleSubmit(onSubmit, onErrors)}>
                <FormElementGroup label={t('comment.comment')} element={(id) => <TextArea id={id} rows={3} {...register('body')} />} />
                <Button type={'submit'} buttonType="primary" title={t('comment.add')}>
                    {t('comment.add')}
                </Button>
            </form>
        </>
    );
}

CommentForm.defaultProps = {
    readonly: false,
    parentId: null
};
