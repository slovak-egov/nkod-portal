import { Fragment } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { applicationTypeCodeList } from '../codelist/ApplicationCodelist';
import CommentButton from '../components/CommentButton';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import LikeButton from '../components/LikeButton';
import { Application } from '../interface/cms.interface';
import { useUserInfo } from '../client';

type Props = {
    app: Application;
    isLast: boolean;
    edit?: boolean;
};

const ApplicationListItem = (props: Props) => {
    const { t } = useTranslation();
    const [userInfo] = useUserInfo();
    const { app, isLast, edit } = props;

    return (
        <Fragment key={app.id}>
            <GridRow data-testid="sr-result">
                <GridColumn widthUnits={1} totalUnits={1}>
                    <GridRow>
                        <GridColumn widthUnits={1} totalUnits={2}>
                            <Link to={'/aplikacia/' + app.id} className="idsk-card-title govuk-link">
                                {app.title}
                            </Link>
                        </GridColumn>
                        <GridColumn widthUnits={1} totalUnits={2} flexEnd>
                            {edit && userInfo?.id && (
                                <Link to={`/aplikacia/${app.id}/upravit`} className="idsk-card-title govuk-link">
                                    {t('common.edit')}
                                </Link>
                            )}
                            <LikeButton count={app.likeCount} contentId={app.id} url={`cms/applications/likes`} />
                            <Link to={`/aplikacia/${app.id}/komentare`} className="no-link">
                                <CommentButton count={app.commentCount} />
                            </Link>
                        </GridColumn>
                    </GridRow>
                </GridColumn>
                {app.description && (
                    <GridColumn widthUnits={1} totalUnits={1}>
                        <div
                            style={{
                                WebkitLineClamp: 3,
                                WebkitBoxOrient: 'vertical',
                                overflow: 'hidden',
                                textOverflow: 'ellipsis',
                                display: '-webkit-box'
                            }}
                        >
                            {app.description}
                        </div>
                    </GridColumn>
                )}
                {app.type && (
                    <>
                        <GridColumn widthUnits={1} totalUnits={2}>
                            <span style={{ color: '#000', fontWeight: 'bold' }}>{applicationTypeCodeList?.find((type) => type.id === app.type)?.label}</span>
                        </GridColumn>
                    </>
                )}
            </GridRow>
            {!isLast ? <hr className="idsk-search-results__card__separator" /> : null}
        </Fragment>
    );
};

export default ApplicationListItem;

ApplicationListItem.defaultProps = {
    edit: true
};
